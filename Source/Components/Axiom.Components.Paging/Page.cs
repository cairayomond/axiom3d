﻿#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Math;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	public class Page : DisposableObject, WorkQueue.IRequestHandler, WorkQueue.IResponseHandler
	{
		public static uint CHUNK_ID = StreamSerializer.MakeIdentifier( "PAGE" );
		public static ushort CHUNK_VERSION = 1;
		public static uint CHUNK_CONTENTCOLLECTION_DECLARATION_ID = StreamSerializer.MakeIdentifier( "PCNT" );
		public static ushort WORKQUEUE_PREPARE_REQUEST = 1;
		public static ushort WORKQUEUE_CHANGECOLLECTION_REQUEST = 3;

		protected PageID mID;
		protected PagedWorldSection mParent;
		protected int mFrameLastHeld;
		protected List<PageContentCollection> mContentCollections = new List<PageContentCollection>();
		protected SceneNode mDebugNode;
		protected bool mDeferredProcessInProgress;
		protected bool mModified;
		protected ushort workQueueChannel;

		protected struct PageData
		{
			public List<PageContentCollection> collectionsToAdd;
		};

		/// <summary>
		/// Structure for holding background page requests
		/// </summary>
		protected struct PageRequest
		{
			public Page srcPage;

			public PageRequest( Page p )
			{
				this.srcPage = p;
			}
		};

		protected struct PageResponse
		{
			public PageData pageData;
		};

		/// <summary>
		/// Get the ID of this page, unique withing the parent
		/// </summary>
		public virtual PageID PageID
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mID;
			}
		}

		/// <summary>
		///  Get the PagedWorldSection this page belongs to
		/// </summary>
		public virtual PagedWorldSection ParentSection
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mParent;
			}
		}

		/// <summary>
		/// Get the frame number in which this Page was last loaded or held.
		/// </summary>
		/// <remarks>
		/// A Page that has not been requested to be loaded or held in the recent
		///	past will be a candidate for removal. 
		/// </remarks>
		public virtual int FrameLastHeld
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mFrameLastHeld;
			}
		}

		public PageManager Manager
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mParent.Manager;
			}
		}

		/// <summary>
		/// Get the number of content collections
		/// </summary>
		public virtual int ContentCollectionCount
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mContentCollections.Count;
			}
		}

		/// <summary>
		/// Get the list of content collections
		/// </summary>
		public virtual List<PageContentCollection> ContentCollectionList
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mContentCollections;
			}
		}

		/// <summary>
		/// Returns whether this page was 'held' in the last frame, that is
		/// was it either directly needed, or requested to stay in memory (held - as
		/// in a buffer region for example). If not, this page is eligible for 
		/// removal.
		/// </summary>
		public virtual bool IsHeld
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				int nextFrame = Root.Instance.NextFrameNumber;
				int dist;

				if ( nextFrame < this.mFrameLastHeld )
				{
					// we must have wrapped around
					dist = this.mFrameLastHeld + ( int.MaxValue - this.mFrameLastHeld );
				}
				else
				{
					dist = nextFrame - this.mFrameLastHeld;
				}

				// 5-frame tolerance
				return dist <= 5;
			}
		}

		public SceneManager SceneManager
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mParent.SceneManager;
			}
		}

		/// <summary>
		/// If true, it's not safe to access this Page at this time, contents may be changing
		/// </summary>
		public bool IsDeferredProcessInProgress
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mDeferredProcessInProgress;
			}
		}

		/// <summary>
		/// Default Constructor
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public Page( PageID pageID, PagedWorldSection parent )
			: base()
		{
			this.mID = pageID;
			this.mParent = parent;

			WorkQueue wq = Root.Instance.WorkQueue;
			this.workQueueChannel = wq.GetChannel( "Axiom/Page" );
			wq.AddRequestHandler( this.workQueueChannel, this );
			wq.AddResponseHandler( this.workQueueChannel, this );
			Touch();
		}

		[OgreVersion( 1, 7, 2, "~Page" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					WorkQueue wq = Root.Instance.WorkQueue;
					wq.RemoveRequestHandler( this.workQueueChannel, this );
					wq.RemoveResponseHandler( this.workQueueChannel, this );

					DestroyAllContentCollections();

					if ( this.mDebugNode != null )
					{
						// destroy while we have the chance
						for ( int i = 0; i < this.mDebugNode.ObjectCount; ++i )
						{
							this.mParent.SceneManager.DestroyMovableObject( this.mDebugNode.GetObject( i ) );
						}

						this.mDebugNode.RemoveAndDestroyAllChildren();
						this.mParent.SceneManager.DestroySceneNode( this.mDebugNode );

						this.mDebugNode = null;
					}
				}
			}

			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// Destroy all PageContentCollections within this page.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void DestroyAllContentCollections()
		{
			foreach ( var i in this.mContentCollections )
			{
				if ( !i.IsDisposed )
				{
					i.SafeDispose();
				}
			}
			this.mContentCollections.Clear();
		}

		/// <summary>
		/// 'Touch' the page to let it know it's being used
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void Touch()
		{
			this.mFrameLastHeld = Root.Instance.NextFrameNumber;
		}

		[OgreVersion( 1, 7, 2 )]
		protected virtual bool PrepareImpl( StreamSerializer stream, ref PageData dataToPopulate )
		{
			//now do the real loading
			if ( stream.ReadChunkBegin( CHUNK_ID, CHUNK_VERSION, "Page" ) == null )
			{
				return false;
			}

			// pageID check (we should know the ID we're expecting)
			int storedID = -1;
			stream.Read( out storedID );
			if ( this.mID.Value != storedID )
			{
				LogManager.Instance.Write( "Error: Tried to populate Page ID {0} with data corresponding to page ID {1}",
				                           this.mID.Value,
				                           storedID );
				stream.UndoReadChunk( CHUNK_ID );
				return false;
			}

			PageManager mgr = Manager;

			while ( stream.NextChunkId == Page.CHUNK_CONTENTCOLLECTION_DECLARATION_ID )
			{
				Chunk collChunk = stream.ReadChunkBegin();
				string factoryName;
				stream.Read( out factoryName );
				stream.ReadChunkEnd( CHUNK_CONTENTCOLLECTION_DECLARATION_ID );
				//Supported type?
				IPageContentCollectionFactory collFact = mgr.GetContentCollectionFactory( factoryName );
				if ( collFact != null )
				{
					PageContentCollection collInst = collFact.CreateInstance();
					if ( collInst.Prepare( stream ) )
					{
						dataToPopulate.collectionsToAdd.Add( collInst );
					}
					else
					{
						LogManager.Instance.Write( "Error preparing PageContentCollection type: {0} in {1}", factoryName, ToString() );
						collFact.DestroyInstance( ref collInst );
					}
				}
				else
				{
					LogManager.Instance.Write( "Unsupported PageContentCollection type: {0} in {1}", factoryName, ToString() );
					//skip
					stream.ReadChunkEnd( collChunk.id );
				}
			}

			this.mModified = false;
			return true;
		}

		/// <summary>
		/// Load this page.
		/// </summary>
		/// <param name="synchronous">Whether to force this to happen synchronously.</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void Load( bool synchronous )
		{
			if ( !this.mDeferredProcessInProgress )
			{
				DestroyAllContentCollections();
				var req = new PageRequest( this );
				this.mDeferredProcessInProgress = true;

				Root.Instance.WorkQueue.AddRequest( this.workQueueChannel, WORKQUEUE_PREPARE_REQUEST, req, 0, synchronous );
			}
		}

		/// <summary>
		/// Unload this page.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void UnLoad()
		{
			DestroyAllContentCollections();
		}

		[OgreVersion( 1, 7, 2 )]
		protected virtual bool PrepareImpl( ref PageData dataToPopulate )
		{
			// Procedural preparation
			if ( this.mParent.PrepareProcedualePage( this ) )
			{
				return true;
			}
			else
			{
				// Background loading
				string filename = GenerateFilename();

				var stream = Root.Instance.OpenFileStream( filename, Manager.PageResourceGroup );
				var ser = new StreamSerializer( stream );
				return PrepareImpl( ser, ref dataToPopulate );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected virtual void LoadImpl()
		{
			this.mParent.LoadProcedualPage( this );
			foreach ( var i in this.mContentCollections )
			{
				i.Load();
			}
		}

		/// <summary>
		/// Save page data to an automatically generated file name
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void Save()
		{
			string filename = GenerateFilename();
			Save( filename );
		}

		/// <summary>
		/// Save page data to a file
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void Save( string fileName )
		{
			var stream = Root.Instance.CreateFileStream( fileName, Manager.PageResourceGroup, true );
			var ser = new StreamSerializer( stream );
			Save( ser );
		}

		/// <summary>
		/// Save page data to a serializer
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void Save( StreamSerializer stream )
		{
			stream.WriteChunkBegin( CHUNK_ID, CHUNK_VERSION );

			//page id
			stream.Write( this.mID.Value );

			//content collections
			foreach ( var coll in this.mContentCollections )
			{
				//declaration
				stream.WriteChunkBegin( CHUNK_CONTENTCOLLECTION_DECLARATION_ID );
				stream.Write( coll.Type );
				stream.WriteChunkEnd( CHUNK_CONTENTCOLLECTION_DECLARATION_ID );
				//data
				coll.Save( stream );
			}

			stream.WriteChunkEnd( CHUNK_ID );
			this.mModified = false;
		}

		/// <summary>
		/// Called when the frame starts
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void FrameStart( Real timeSinceLastFrame )
		{
			UpdateDebugDisplay();

			// content collections
			foreach ( var coll in this.mContentCollections )
			{
				coll.FrameStart( timeSinceLastFrame );
			}
		}

		/// <summary>
		/// Called when the frame ends
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void FrameEnd( Real timeElapsed )
		{
			// content collections
			foreach ( var coll in this.mContentCollections )
			{
				coll.FrameEnd( timeElapsed );
			}
		}

		/// <summary>
		/// Notify a section of the current camera
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void NotifyCamera( Camera cam )
		{
			// content collections
			foreach ( var coll in this.mContentCollections )
			{
				coll.NotifyCamera( cam );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void UpdateDebugDisplay()
		{
			byte dbglvl = Manager.DebugDisplayLevel;
			if ( dbglvl > 0 )
			{
				// update debug display
				if ( this.mDebugNode != null )
				{
					this.mDebugNode = this.mParent.SceneManager.RootSceneNode.CreateChildSceneNode();
				}

				this.mParent.Strategy.UpdateDebugDisplay( this, this.mDebugNode );
				this.mDebugNode.IsVisible = true;
			}
			else if ( this.mDebugNode != null )
			{
				this.mDebugNode.IsVisible = false;
			}
		}

		/// <summary>
		/// Create a new PageContentCollection within this page.
		/// This is equivalent to calling PageManager.CreateContentCollection and 
		/// then attachContentCollection.
		/// </summary>
		/// <param name="typeName">The name of the type of content collection (see PageManager.GetContentCollectionFactories)</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual PageContentCollection CreateContentCollection( string typeName )
		{
			PageContentCollection coll = Manager.CreateContentCollection( typeName );
			coll.NotifyAttached( this );
			this.mContentCollections.Add( coll );
			return coll;
		}

		/// <summary>
		/// Destroy a PageContentCollection within this page.
		/// This is equivalent to calling DetachContentCollection and 
		///	PageManager.DestroyContentCollection.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void DestroyContentCollection( ref PageContentCollection coll )
		{
			if ( this.mContentCollections.Contains( coll ) )
			{
				this.mContentCollections.Remove( coll );
			}

			Manager.DestroyContentCollection( ref coll );
		}

		/// <summary>
		/// Get a content collection
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual PageContentCollection GetContentCollection( int index )
		{
			System.Diagnostics.Debug.Assert( index < this.mContentCollections.Count );
			return this.mContentCollections[ index ];
		}

		[OgreVersion( 1, 7, 2, "operator <<" )]
		public override string ToString()
		{
			return string.Format( "Page( ID: {0}, section: {1}, world: {2})", this.mID, ParentSection.Name,
			                      ParentSection.World.Name );
		}

		[OgreVersion( 1, 7, 2 )]
		protected string GenerateFilename()
		{
			var str = new StringBuilder();
			if ( this.mParent != null )
			{
				str.AppendFormat( "{0}_{1}", this.mParent.World.Name, this.mParent.Name );
			}

			str.AppendFormat( "{0}.page", this.mID.Value.ToString( "X" ).PadLeft( 8, '0' ) );
			return str.ToString();
		}

		#region IRequestHandler Members

		[OgreVersion( 1, 7, 2 )]
		/// <see cref="WorkQueue.IRequestHandler.CanHandleRequest"/>
		public bool CanHandleRequest( WorkQueue.Request req, WorkQueue srcQ )
		{
			var preq = (PageRequest)req.Data;
			// only deal with own requests
			// we do this because if we delete a page we want any pending tasks to be discarded
			if ( preq.srcPage != this )
			{
				return false;
			}
			else
			{
				return !req.Aborted;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		/// <see cref="WorkQueue.IRequestHandler.HandleRequest"/>
		public WorkQueue.Response HandleRequest( WorkQueue.Request req, WorkQueue srcQ )
		{
			// Background thread (maybe)

			var preq = (PageRequest)req.Data;
			// only deal with own requests; we shouldn't ever get here though
			if ( preq.srcPage != this )
			{
				return null;
			}

			var res = new PageResponse();
			res.pageData = new PageData();
			WorkQueue.Response response;
			try
			{
				PrepareImpl( ref res.pageData );
				response = new WorkQueue.Response( req, true, res );
			}
			catch ( Exception e )
			{
				// oops
				response = new WorkQueue.Response( req, false, res, e.Message );
			}

			return response;
		}

		#endregion IRequestHandler Members

		#region IResponseHandler Members

		[OgreVersion( 1, 7, 2 )]
		/// <see cref="WorkQueue.IResponseHandler.CanHandleResponse"/>
		public bool CanHandleResponse( WorkQueue.Response res, WorkQueue srcq )
		{
			var preq = (PageRequest)res.Request.Data;
			// only deal with own requests
			// we do this because if we delete a page we want any pending tasks to be discarded
			if ( preq.srcPage != this )
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		/// <see cref="WorkQueue.IResponseHandler.HandleResponse"/>
		public void HandleResponse( WorkQueue.Response res, WorkQueue srcq )
		{
			// Main thread
			var pres = (PageResponse)res.Data;
			var preq = (PageRequest)res.Request.Data;

			// only deal with own requests
			if ( preq.srcPage != this )
			{
				return;
			}

			// final loading behaviour
			if ( res.Succeeded )
			{
				Utility.Swap<List<PageContentCollection>>( ref this.mContentCollections, ref pres.pageData.collectionsToAdd );
				LoadImpl();
			}

			this.mDeferredProcessInProgress = false;
		}

		#endregion IResponseHandler Members
	};
}