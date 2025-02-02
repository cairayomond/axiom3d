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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using Axiom.Core;
using Axiom.Scripting;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

// ReSharper disable InconsistentNaming

namespace Axiom.Graphics
{
	/// <summary>
	/// Defines a program which runs on the GPU such as a vertex or fragment program.
	/// </summary>
	public abstract class GpuProgram : Resource
	{
		#region Fields and Properties

		#region BindingDelegate Property

		/// <summary>
		///    Returns the GpuProgram which should be bound to the pipeline.
		/// </summary>
		/// <remarks>
		///    This method is simply to allow some subclasses of GpuProgram to delegate
		///    the program which is bound to the pipeline to a delegate, if required.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual GpuProgram BindingDelegate
		{
			get
			{
				return this;
			}
		}

		#endregion BindingDelegate Property

		/// <summary>
		///    Whether this source is being loaded from file or not.
		/// </summary>
		protected bool LoadFromFile { get; set; }

		/// <summary>
		/// Returns a string that specifies the language of the gpu programs as specified
		/// in a material script. ie: asm, cg, hlsl, glsl
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual string Language
		{
			get
			{
				return "asm";
			}
		}

		#region SourceFile Property

		/// <summary>
		///    The name of the file to load from source (may be blank).
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected string fileName;

		/// <summary>
		///    Gets/Sets the source file for this program.
		/// </summary>
		/// <remarks>
		///    Setting this will have no effect until you (re)load the program.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual string SourceFile
		{
			get
			{
				return this.fileName;
			}
			set
			{
				this.fileName = value;
				this.source = "";
				LoadFromFile = true;
				this.compileError = false;
			}
		}

		#endregion SourceFile Property

		#region Source Property

		/// <summary>
		///    The assembler source of this program.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected string source;

		/// <summary>
		///    Gets/Sets the source assembler code for this program.
		/// </summary>
		/// <remarks>
		///    Setting this will have no effect until you (re)load the program.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual string Source
		{
			get
			{
				return this.source;
			}
			set
			{
				this.source = value;
				this.fileName = "";
				LoadFromFile = false;
				this.compileError = false;
			}
		}

		#endregion Source Property

		#region SyntaxCode Property

		[OgreVersion( 1, 7, 2790 )] protected string syntaxCode;

		/// <summary>
		///   Syntax code (i.e. arbvp1, vs_2_0, etc.)
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual string SyntaxCode
		{
			get
			{
				return this.syntaxCode;
			}
			set
			{
				this.syntaxCode = value;
			}
		}

		#endregion SyntaxCode Property

		#region Type Property

		[OgreVersion( 1, 7, 2790 )] protected GpuProgramType type;

		/// <summary>
		///   Type of program this represents (vertex or fragment).
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual GpuProgramType Type
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}

		#endregion Type Property

		#region IsSkeletalAnimationIncluded Property

		[OgreVersion( 1, 7, 2790 )] protected bool isSkeletalAnimationIncluded;

		/// <summary>
		///        Flag indicating whether this program is being used for hardware skinning.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool IsSkeletalAnimationIncluded
		{
			get
			{
				return this.isSkeletalAnimationIncluded;
			}
			set
			{
				this.isSkeletalAnimationIncluded = value;
			}
		}

		#endregion IsSkeletalAnimationIncluded Property

		#region IsMorphAninimationIncluded Property

		/// <summary>
		///        Does this (vertex) program include morph animation?
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected bool morphAnimation;

		/// <summary>
		///        Gets/Sets whether a vertex program includes the required instructions
		///        to perform morph animation. 
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool IsMorphAnimationIncluded
		{
			get
			{
				return this.morphAnimation;
			}
			set
			{
				this.morphAnimation = value;
			}
		}

		#endregion IsMorphAninimationIncluded Property

		#region IsVertexTextureFetchRequired Property

		/// <summary>
		///        Does this (vertex) program require vertex texture fetch?
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected bool vertexTextureFetchRequired;

		/// <summary>
		///        Gets/Sets whether this vertex program requires support for vertex 
		///        texture fetch from the hardware. 
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool IsVertexTextureFetchRequired
		{
			get
			{
				return this.vertexTextureFetchRequired;
			}
			set
			{
				this.vertexTextureFetchRequired = value;
			}
		}

		#endregion IsVertexTextureFetchRequired Property

		#region IsAdjacencyInfoRequired Property

		/// <summary>
		///        Does this (vertex) program require vertex texture fetch?
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected bool needsAdjacencyInfo;

		/// <summary>
		///        Gets/Sets whether this vertex program requires support for vertex 
		///        texture fetch from the hardware. 
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool IsAdjacencyInfoRequired
		{
			get
			{
				return this.needsAdjacencyInfo;
			}
			set
			{
				this.needsAdjacencyInfo = value;
			}
		}

		#endregion IsAdjacencyInfoRequired Property

		#region PoseAnimationCount Property

		/// <summary>
		///        Does this (vertex) program include morph animation?
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected ushort poseAnimation;

		/// <summary>
		///        Gets/Sets whether a vertex program includes the required instructions
		///        to perform pose animation. 
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual ushort PoseAnimationCount
		{
			get
			{
				return this.poseAnimation;
			}
			set
			{
				this.poseAnimation = value;
			}
		}

		#endregion PoseAnimationCount Property

		#region IsPoseAnimationIncluded Property

		/// <summary>
		/// 
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool IsPoseAnimationIncluded
		{
			get
			{
				return this.poseAnimation > 0;
			}
		}

		#endregion SyntaxCode Property

		#region DefaultParameters Property

		/// <summary>
		///    List of default parameters, as gathered from the program definition.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected GpuProgramParameters defaultParams;

		/// <summary>
		///    Get a reference to the default parameters which are to be used for all uses of this program.
		/// </summary>
		/// <remarks>
		/// A program can be set up with a list of default parameters, which can save time when 
		/// using a program many times in a material with roughly the same settings. By 
		/// retrieving the default parameters and populating it with the most used options, 
		/// any new parameter objects created from this program afterwards will automatically include
		/// the default parameters; thus users of the program need only change the parameters
		/// which are unique to their own usage of the program.
		/// </remarks>
		public virtual GpuProgramParameters DefaultParameters
		{
			get
			{
				return this.defaultParams ?? ( this.defaultParams = CreateParameters() );
			}
		}

		#endregion DefaultParameters Property

		#region HasDefaultParameters Property

		[OgreVersion( 1, 7, 2790 )]
		public virtual bool HasDefaultParameters
		{
			get
			{
				return this.defaultParams != null;
			}
		}

		#endregion HasDefaultParameters Property

		#region PassSurfaceAndLightStates Property

		/// <summary>
		///        Determines whether a vertex program requires light and material states to be passed
		///        to through fixed pipeline low level API rendering calls.
		/// </summary>
		/// <remarks>
		///        If this is set to true, Axiom will pass all active light states to the fixed function
		///        pipeline.  This is useful for high level shaders like GLSL that can read the OpenGL
		///        light and material states.  This way the user does not have to use autoparameters to 
		///        pass light position, color etc.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool PassSurfaceAndLightStates
		{
			get
			{
				return false;
			}
		}

		#endregion PassSurfaceAndLightStates Property

		#region PassFogStates Property

		/// <summary>
		///        Determines whether a vertex program requires fog states to be passed
		///        to through fixed pipeline low level API rendering calls.
		/// </summary>
		/// <remarks>
		///        If this is set to true, Axiom will pass all fog states to the fixed function
		///        pipeline.  This is useful for high level shaders like GLSL that can read the OpenGL
		///        fog states.  This way the user does not have to use autoparameters to 
		///        pass fog color etc.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool PassFogStates
		{
			get
			{
				return true;
			}
		}

		#endregion PassFogStates Property

		#region PassTransformStates Property

		/// <summary>
		///        Sets whether a vertex program requires transform states to be passed
		///        to through fixed pipeline low level API rendering calls.
		/// </summary>
		/// <remarks>
		///        If this is set to true, Axiom will pass all transform states to the fixed function
		///        pipeline.  This is useful for high level shaders like GLSL that can read the OpenGL
		///        transform states.  This way the user does not have to use autoparameters to 
		///        pass position etc.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool PassTransformStates
		{
			get
			{
				return false;
			}
		}

		#endregion PassTransformStates Property

		#region IsSupported Property

		/// <summary>
		///    Returns whether this program can be supported on the current renderer and hardware.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool IsSupported
		{
			get
			{
				if ( this.compileError || !IsRequiredCapabilitiesSupported() )
				{
					return false;
				}

				return GpuProgramManager.Instance.IsSyntaxSupported( SyntaxCode );
			}
		}

		#endregion IsSupported Property

		#region CompilerError Property

		/// <summary>
		/// Did we encounter a compilation error?
		/// </summary>
		protected bool compileError;

		/// <summary>
		/// Did this program encounter a compile error when loading?
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool HasCompileError
		{
			get
			{
				return this.compileError;
			}
		}

		/// <summary>
		/// Reset a compile error if it occurred, allowing the load to be retried.
		/// </summary>
		public virtual void ResetCompileError()
		{
			this.compileError = false;
		}

		#endregion CompilerError Property

		/// <summary>
		/// Record of logical to physical buffer maps. Mandatory for low-level
		/// programs or high-level programs which set their params the same way.
		/// </summary>
		protected GpuProgramParameters.GpuLogicalBufferStruct floatLogicalToPhysical;

		/// <summary>
		/// Record of logical to physical buffer maps. Mandatory for low-level
		/// programs or high-level programs which set their params the same way.
		/// </summary>
		protected GpuProgramParameters.GpuLogicalBufferStruct intLogicalToPhysical;

		#region ConstantDefinitions Property

		/// <summary>
		/// Parameter name -> ConstantDefinition map, shared instance used by all parameter objects
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected GpuProgramParameters.GpuNamedConstants constantDefs;

		/// <summary>
		/// Get the full list of named constants.
		/// </summary>
		/// <note>
		/// Only available if this parameters object has named parameters, which means either
		/// a high-level program which loads them, or a low-level program which has them
		/// specified manually.
		/// </note>
		[OgreVersion( 1, 7, 2790 )]
		public virtual GpuProgramParameters.GpuNamedConstants ConstantDefinitions
		{
			get
			{
				return this.constantDefs;
			}
		}

		#endregion ConstantDefinitions Property

		#region ManualNamedConstants Property

		/// <summary>
		/// Allows you to manually provide a set of named parameter mappings
		/// to a program which would not be able to derive named parameters itself.
		/// </summary>
		/// <remarks>
		/// You may wish to use this if you have assembler programs that were compiled
		/// from a high-level source, and want the convenience of still being able
		/// to use the named parameters from the original high-level source.
		/// <seealso cref="ManualNamedConstantsFile"/>
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual GpuProgramParameters.GpuNamedConstants ManualNamedConstants
		{
			set
			{
				CreateParameterMappingStructures();
				this.constantDefs = value;

				this.floatLogicalToPhysical.BufferSize = this.constantDefs.FloatBufferSize;
				this.intLogicalToPhysical.BufferSize = this.constantDefs.IntBufferSize;
				this.floatLogicalToPhysical.Map.Clear();
				this.intLogicalToPhysical.Map.Clear();

				// need to set up logical mappings too for some rendersystems
				foreach ( var pair in this.constantDefs.Map )
				{
					var name = pair.Key;
					var def = pair.Value;
					// only consider non-array entries
					if ( !name.Contains( "[" ) )
					{
						continue;
					}

					var val = new GpuProgramParameters.GpuLogicalIndexUse( def.PhysicalIndex, def.ArraySize*def.ElementSize,
					                                                       def.Variability );
					if ( def.IsFloat )
					{
						this.floatLogicalToPhysical.Map.Add( def.LogicalIndex, val );
					}
					else
					{
						this.intLogicalToPhysical.Map.Add( def.LogicalIndex, val );
					}
				}
			}
		}

		#endregion

		public virtual GpuProgramParameters.GpuNamedConstants NamedConstants
		{
			get
			{
				return this.constantDefs;
			}
		}

		#region ManualNamedConstantsFile Property

		/// <summary>   
		/// File from which to load named constants manually
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected string manualNamedConstantsFile;

		/// <summary>
		/// Specifies the name of a file from which to load named parameters mapping
		/// for a program which would not be able to derive named parameters itself.
		/// </summary>
		/// <remarks>
		/// You may wish to use this if you have assembler programs that were compiled
		/// from a high-level source, and want the convenience of still being able
		/// to use the named parameters from the original high-level source. This
		/// method will make a low-level program search in the resource group of the
		/// program for the named file from which to load parameter names from. 
		/// The file must be in the format produced by <see>GpuNamedConstants.Save</see>.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual string ManualNamedConstantsFile
		{
			get
			{
				return this.manualNamedConstantsFile;
			}
			set
			{
				this.manualNamedConstantsFile = value;
				this._loadedManualNamedConstants = false;
			}
		}

		#endregion ManualNamedConstantsFile Property

		[OgreVersion( 1, 7, 2790 )] private bool _loadedManualNamedConstants;

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///    Constructor for creating
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected GpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual,
		                      IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			this.type = GpuProgramType.Vertex;
			LoadFromFile = true;
			this.isSkeletalAnimationIncluded = false;
			this.morphAnimation = false;
			this.poseAnimation = 0;
			this.vertexTextureFetchRequired = false;
			this.needsAdjacencyInfo = false;
			this.compileError = false;
			this._loadedManualNamedConstants = false;

			CreateParameterMappingStructures();
		}

		#endregion Construction and Destruction

		#region Methods

		#region CreateParameters

		/// <summary>
		///    Creates a new parameters object compatible with this program definition.
		/// </summary>
		/// <remarks>
		///    It is recommended that you use this method of creating parameters objects
		///    rather than going direct to GpuProgramManager, because this method will
		///    populate any implementation-specific extras (like named parameters) where
		///    they are appropriate.
		/// </remarks>
		/// <returns></returns>
		[OgreVersion( 1, 7, 2790 )]
		public virtual GpuProgramParameters CreateParameters()
		{
			// Default implementation simply returns standard parameters.
			var ret = GpuProgramManager.Instance.CreateParameters();

			// optionally load manually supplied named constants
			if ( !String.IsNullOrEmpty( this.manualNamedConstantsFile ) && !this._loadedManualNamedConstants )
			{
				try
				{
					var namedConstants = new GpuProgramParameters.GpuNamedConstants();
					var stream = ResourceGroupManager.Instance.OpenResource( this.manualNamedConstantsFile, Group, true, this );
					namedConstants.Load( stream );
					ManualNamedConstants = namedConstants;
				}
				catch ( Exception ex )
				{
					LogManager.Instance.Write( "Unable to load manual named constants for GpuProgram {0} : {1}", Name,
					                           LogManager.BuildExceptionString( ex ) );
				}
				this._loadedManualNamedConstants = true;
			}

			// set up named parameters, if any
			if ( this.constantDefs != null && this.constantDefs.Map.Count > 0 )
			{
				ret.NamedConstants = this.constantDefs;
			}
			// link shared logical / physical map for low-level use
			ret.SetLogicalIndexes( this.floatLogicalToPhysical, this.intLogicalToPhysical );


			// Copy in default parameters if present
			if ( this.defaultParams != null )
			{
				ret.CopyConstantsFrom( this.defaultParams );
			}

			return ret;
		}

		#endregion

		#region load

		/// <summary>
		///    Loads this Gpu Program.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "original name loadImpl" )]
		protected override void load()
		{
			// load from file and get the source string from it
			if ( LoadFromFile )
			{
				var stream = ResourceGroupManager.Instance.OpenResource( this.fileName, Group, true, this );
				var reader = new StreamReader( stream, System.Text.Encoding.UTF8 );
				this.source = reader.ReadToEnd();
			}

			// Call polymorphic load
			try
			{
				LoadFromSource();

				if ( this.defaultParams != null )
				{
					// Keep a reference to old ones to copy
					var savedParams = this.defaultParams;
					// reset params to stop them being referenced in the next create
					// _defaultParams.SetNull();

					// Create new params
					this.defaultParams = CreateParameters();

					// Copy old (matching) values across
					// Don't use copyConstantsFrom since program may be different
					this.defaultParams.CopyMatchingNamedConstantsFrom( savedParams );
				}
			}
			catch ( Exception ex )
			{
				LogManager.Instance.Write(
					"Gpu program {0} encountered an error during loading and is thus not supported. Details: {1}", Name, ex.Message );
				this.compileError = true;
			}
		}

		#endregion

		[OgreVersion( 0, 0, 0, "not overriden in 1.7.2790" )]
		protected override void unload()
		{
		}

		/// <summary>
		///    Method which must be implemented by subclasses, loads the program from source.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected abstract void LoadFromSource();

		#region IsRequiredCapabilitiesSupported

		[OgreVersion( 1, 7, 2790 )]
		protected bool IsRequiredCapabilitiesSupported()
		{
			var caps = Root.Instance.RenderSystem.Capabilities;
			// If skeletal animation is being done, we need support for UBYTE4
			if ( IsSkeletalAnimationIncluded && !caps.HasCapability( Capabilities.VertexFormatUByte4 ) )
			{
				return false;
			}

			// Vertex texture fetch required?
			return !IsVertexTextureFetchRequired || caps.HasCapability( Capabilities.VertexTextureFetch );
		}

		#endregion

		#region CreateParameterMappingStructures

		/// <summary>
		/// Create the internal params logical &amp; named mapping structures
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected void CreateParameterMappingStructures()
		{
			CreateParameterMappingStructures( true );
		}

		/// <summary>
		/// Create the internal params logical &amp; named mapping structures
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected void CreateParameterMappingStructures( bool recreateIfExists )
		{
			CreateLogicalParameterMappingStructures( recreateIfExists );
			CreateNamedParameterMappingStructures( recreateIfExists );
		}

		#endregion

		#region CreateLogicalParameterMappingStructures

		/// <summary>
		/// Create the internal params logical mapping structures
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected void CreateLogicalParameterMappingStructures( bool recreateIfExists )
		{
			if ( recreateIfExists || this.floatLogicalToPhysical == null )
			{
				this.floatLogicalToPhysical = new GpuProgramParameters.GpuLogicalBufferStruct();
			}
			if ( recreateIfExists || this.intLogicalToPhysical == null )
			{
				this.intLogicalToPhysical = new GpuProgramParameters.GpuLogicalBufferStruct();
			}
		}

		#endregion

		#region CreateNamedParameterMappingStructures

		/// <summary>
		/// Create the internal params named mapping structures
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private void CreateNamedParameterMappingStructures( bool recreateIfExists )
		{
			if ( recreateIfExists || this.constantDefs == null )
			{
				this.constantDefs = new GpuProgramParameters.GpuNamedConstants();
			}
		}

		#endregion

		#endregion

		#region Custom Parameters

		#region TypePropertyCommand

		[ScriptableProperty( "type", "'vertex_program', 'geometry_program' or 'fragment_program'" )]
		[OgreVersion( 1, 7, 2790 )]
		public class TypePropertyCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			[OgreVersion( 1, 7, 2790 )]
			public string Get( object t )
			{
				switch ( ( (GpuProgram)t ).Type )
				{
					case GpuProgramType.Vertex:
						return "vertex_program";
					case GpuProgramType.Geometry:
						return "geometry_program";
					default:
						return "fragment_program";
				}
			}

			[OgreVersion( 1, 7, 2790 )]
			public void Set( object target, string val )
			{
				switch ( val )
				{
					case "vertex_program":
						( (GpuProgram)target ).Type = GpuProgramType.Vertex;
						break;

					case "geometry_program":
						( (GpuProgram)target ).Type = GpuProgramType.Geometry;
						break;

					default:
						( (GpuProgram)target ).Type = GpuProgramType.Fragment;
						break;
				}
			}

			#endregion
		}

		#endregion

		#region SyntaxPropertyCommand

		[ScriptableProperty( "syntax", "Syntax code, e.g. vs_1_1" )]
		[OgreVersion( 1, 7, 2790 )]
		public class SyntaxPropertyCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			[OgreVersion( 1, 7, 2790 )]
			public string Get( object t )
			{
				return ( (GpuProgram)t ).SyntaxCode;
			}

			[OgreVersion( 1, 7, 2790 )]
			public void Set( object t, string val )
			{
				( (GpuProgram)t ).SyntaxCode = val;
			}

			#endregion
		}

		#endregion

		#region IncludesSkeletalAnimationPropertyCommand

		[OgreVersion( 1, 7, 2790 )]
		[ScriptableProperty( "includes_skeletal_animation", "Whether this vertex program includes skeletal animation",
			typeof ( GpuProgram ) )]
		public class IncludesSkeletalAnimationPropertyCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			[OgreVersion( 1, 7, 2790 )]
			public string Get( object target )
			{
				return ( (GpuProgram)target ).IsSkeletalAnimationIncluded.ToString();
			}

			[OgreVersion( 1, 7, 2790 )]
			public void Set( object target, string val )
			{
				( (GpuProgram)target ).IsSkeletalAnimationIncluded = bool.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

		#endregion

		#region IncludesMorphAnimationPropertyCommand

		[OgreVersion( 1, 7, 2790 )]
		[ScriptableProperty( "includes_morph_animation", "Whether this vertex program includes morph animation" )]
		public class IncludesMorphAnimationPropertyCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			[OgreVersion( 1, 7, 2790 )]
			public string Get( object target )
			{
				return ( (GpuProgram)target ).IsMorphAnimationIncluded.ToString();
			}

			[OgreVersion( 1, 7, 2790 )]
			public void Set( object target, string val )
			{
				( (GpuProgram)target ).IsMorphAnimationIncluded = bool.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

		#endregion

		#region IncludesPoseAnimationPropertyCommand

		[OgreVersion( 1, 7, 2790 )]
		[ScriptableProperty( "includes_pose_animation", "The number of poses this vertex program supports for pose animation"
			)]
		public class IncludesPoseAnimationPropertyCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			[OgreVersion( 1, 7, 2790 )]
			public string Get( object target )
			{
				return ( (GpuProgram)target ).poseAnimation.ToString();
			}

			[OgreVersion( 1, 7, 2790 )]
			public void Set( object target, string val )
			{
				( (GpuProgram)target ).poseAnimation = ushort.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

		#endregion

		#region IsVertexTextureFetchRequiredPropertyCommand

		[OgreVersion( 1, 7, 2790 )]
		[ScriptableProperty( "uses_vertex_texture_fetch", "Whether this vertex program requires vertex texture fetch support."
			)]
		public class IsVertexTextureFetchRequiredPropertyCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			[OgreVersion( 1, 7, 2790 )]
			public string Get( object target )
			{
				return ( (GpuProgram)target ).IsVertexTextureFetchRequired.ToString();
			}

			[OgreVersion( 1, 7, 2790 )]
			public void Set( object target, string val )
			{
				( (GpuProgram)target ).IsVertexTextureFetchRequired = bool.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

		#endregion

		#region ManualNamedConstantsFilePropertyCommand

		[OgreVersion( 1, 7, 2790 )]
		[ScriptableProperty( "manual_named_constants", "File containing named parameter mappings for low-level programs." )]
		public class ManualNamedConstantsFilePropertyCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			[OgreVersion( 1, 7, 2790 )]
			public string Get( object target )
			{
				return ( (GpuProgram)target ).ManualNamedConstantsFile;
			}

			[OgreVersion( 1, 7, 2790 )]
			public void Set( object target, string val )
			{
				( (GpuProgram)target ).ManualNamedConstantsFile = val;
			}

			#endregion IPropertyCommand Members
		}

		#endregion

		#region IsAdjacencyInfoRequiredPropertyCommand

		[OgreVersion( 1, 7, 2790 )]
		[ScriptableProperty( "uses_adjacency_information",
			"Whether this geometry program requires adjacency information from the input primitives." )]
		public class IsAdjacencyInfoRequiredPropertyCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			[OgreVersion( 1, 7, 2790 )]
			public string Get( object t )
			{
				return ( (GpuProgram)t ).IsAdjacencyInfoRequired.ToString();
			}

			[OgreVersion( 1, 7, 2790 )]
			public void Set( object t, string val )
			{
				( (GpuProgram)t ).IsAdjacencyInfoRequired = bool.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

		#endregion

		#endregion Custom Parameters
	}
}

// ReSharper restore InconsistentNaming