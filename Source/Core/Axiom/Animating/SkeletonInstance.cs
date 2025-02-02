#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Animating
{
	/// <summary>
	///		A SkeletonInstance is a single instance of a Skeleton used by a world object.
	/// </summary>
	/// <remarks>
	///		The difference between a Skeleton and a SkeletonInstance is that the
	///		Skeleton is the 'master' version much like Mesh is a 'master' version of
	///		Entity. Many SkeletonInstance objects can be based on a single Skeleton,
	///		and are copies of it when created. Any changes made to this are not
	///		reflected in the master copy. The exception is animations; these are
	///		shared on the Skeleton itself and may not be modified here.
	/// </remarks>
	public class SkeletonInstance : Skeleton
	{
		#region Fields

		/// <summary>
		///		Reference to the master Skeleton.
		/// </summary>
		protected Skeleton skeleton;

		/// <summary>
		///		Used for auto generated tag point handles to ensure they are unique.
		///	</summary>
		protected internal ushort nextTagPointAutoHandle;

		protected AxiomSortedCollection<int, TagPoint> tagPointList = new AxiomSortedCollection<int, TagPoint>();

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor, don't call directly, this will be created automatically
		///		when you create an <see cref="Entity"/> based on a skeletally animated Mesh.
		/// </summary>
		/// <param name="masterCopy"></param>
		public SkeletonInstance( Skeleton masterCopy )
			: base()
		{
			this.skeleton = masterCopy;
		}

		#endregion Constructor

		#region Properties

		/// <summary>
		///     Gets the animations associated with this skeleton
		/// </summary>
		public override ICollection<Animation> Animations
		{
			get
			{
				return this.skeleton.Animations;
			}
		}

		/// <summary>
		///     Gets the master skeleton
		/// </summary>
		public Skeleton MasterSkeleton
		{
			get
			{
				return this.skeleton;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///		Clones bones, for use in cloning the master skeleton to make this a unique
		///		skeleton instance.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="parent"></param>
		protected void CloneBoneAndChildren( Bone source, Bone parent )
		{
			Bone newBone;

			if ( source.Name == "" )
			{
				newBone = CreateBone( source.Handle );
			}
			else
			{
				newBone = CreateBone( source.Name, source.Handle );
			}

			newBone.Orientation = source.Orientation;
			newBone.Position = source.Position;
			newBone.Scale = source.Scale;

			if ( parent == null )
			{
				rootBones.Add( newBone );
			}
			else
			{
				parent.AddChild( newBone );
			}

			// process children
			foreach ( Bone child in source.Children )
			{
				CloneBoneAndChildren( child, newBone );
			}
		}

		public TagPoint CreateTagPointOnBone( Bone bone )
		{
			return CreateTagPointOnBone( bone, Quaternion.Identity );
		}

		public TagPoint CreateTagPointOnBone( Bone bone, Quaternion offsetOrientation )
		{
			return CreateTagPointOnBone( bone, Quaternion.Identity, Vector3.Zero );
		}

		public TagPoint CreateTagPointOnBone( Bone bone, Quaternion offsetOrientation, Vector3 offsetPosition )
		{
			var tagPoint = new TagPoint( ++this.nextTagPointAutoHandle, this );
			this.tagPointList[ this.nextTagPointAutoHandle ] = tagPoint;

			tagPoint.Translate( offsetPosition );
			tagPoint.Rotate( offsetOrientation );
			tagPoint.SetBindingPose();
			bone.AddChild( tagPoint );

			return tagPoint;
		}

		public void FreeTagPoint( TagPoint tagPoint )
		{
			if ( this.tagPointList.ContainsValue( tagPoint ) )
			{
				if ( tagPoint.Parent != null )
				{
					tagPoint.Parent.RemoveChild( tagPoint );
				}
			}
		}

		#endregion Methods

		#region Skeleton Members

		/// <summary>
		///		Creates a new Animation object for animating this skeleton.
		/// </summary>
		/// <remarks>
		///		This method updates the reference skeleton, not just this instance!
		/// </remarks>
		/// <param name="name">The name of this animation.</param>
		/// <param name="length">The length of the animation in seconds.</param>
		/// <returns></returns>
		public override Animation CreateAnimation( string name, float length )
		{
			return this.skeleton.CreateAnimation( name, length );
		}

		/// <summary>
		///		Returns the <see cref="Animation"/> object with the specified name.
		/// </summary>
		/// <param name="name">Name of the animation to retrieve.</param>
		/// <returns>Animation with the specified name, or null if none exists.</returns>
		public override Animation GetAnimation( string name )
		{
			return this.skeleton.GetAnimation( name );
		}

		/// <summary>
		///		Removes an <see cref="Animation"/> from this skeleton.
		/// </summary>
		/// <param name="name">Name of the animation to remove.</param>
		public override void RemoveAnimation( string name )
		{
			this.skeleton.RemoveAnimation( name );
		}

		#endregion Skeleton Members

		#region Resource Members

		/// <summary>
		///		Overriden to copy/clone the bones of the master skeleton.
		/// </summary>
		protected override void load()
		{
			nextAutoHandle = this.skeleton.nextAutoHandle;
			this.nextTagPointAutoHandle = 0;

			BlendMode = this.skeleton.BlendMode;

			// copy bones starting at the roots
			for ( var i = 0; i < this.skeleton.RootBoneCount; i++ )
			{
				var rootBone = this.skeleton.GetRootBone( i );
				CloneBoneAndChildren( rootBone, null );
				rootBone.Update( true, false );
			}

			SetBindingPose();

			// Clone the attachment points
			for ( var i = 0; i < this.skeleton.AttachmentPoints.Count; i++ )
			{
				var ap = this.skeleton.AttachmentPoints[ i ];
				var parentBone = GetBone( ap.ParentBone );
				CreateAttachmentPoint( ap.Name, parentBone.Handle, ap.Orientation, ap.Position );
			}
		}

		/// <summary>
		///		Overriden to unload the skeleton and clear the tagpoint list.
		/// </summary>
		public override void Unload()
		{
			base.Unload();

			// clear all tag points
			this.tagPointList.Clear();
		}

		#endregion Resource Members
	}
}