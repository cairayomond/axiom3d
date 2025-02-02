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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.Graphics.Collections;
using Axiom.Math;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// The type of unit to bind the texture settings to.
	/// </summary>
	public enum TextureBindingType
	{
		/// <summary>
		///  Regular fragment processing unit - the default.
		/// </summary>
		Fragment,

		/// <summary>
		/// Vertex processing unit - indicates this unit will be used for a vertex texture fetch.
		/// </summary>
		Vertex
	}

	/// <summary>
	/// Texture addressing mode for each texture coordinate.
	/// </summary>
	public struct UVWAddressing
	{
		public TextureAddressing U;
		public TextureAddressing V;
		public TextureAddressing W;

		public UVWAddressing( TextureAddressing u, TextureAddressing v, TextureAddressing w )
			: this()
		{
			this.U = u;
			this.V = v;
			this.W = w;
		}

		public UVWAddressing( TextureAddressing commonAddressing )
			: this()
		{
			this.U = this.V = this.W = commonAddressing;
		}

		public override bool Equals( object obj )
		{
			if ( obj == null )
			{
				return false;
			}

			if ( !( obj is UVWAddressing ) )
			{
				return false;
			}

			var a = (UVWAddressing)obj;

			return ( a.U == this.U ) && ( a.V == this.V ) && ( a.W == this.W );
		}

		public override int GetHashCode()
		{
			return this.U.GetHashCode() ^ this.V.GetHashCode() ^ this.W.GetHashCode();
		}

		#region Operators

		public static bool operator ==( UVWAddressing a, UVWAddressing b )
		{
			if ( Object.ReferenceEquals( a, b ) )
			{
				return true;
			}

			return ( a.U == b.U ) && ( a.V == b.V ) && ( a.W == b.W );
		}

		public static bool operator !=( UVWAddressing a, UVWAddressing b )
		{
			return !( a == b );
		}

		#endregion Operators
	}

	/// <summary>
	/// 	Class representing the state of a single texture unit during a Pass of a
	/// 	Technique, of a Material.
	/// </summary>
	/// <remarks> 	
	/// 	Texture units are pipelines for retrieving texture data for rendering onto
	/// 	your objects in the world. Using them is common to both the fixed-function and 
	/// 	the programmable (vertex and fragment program) pipeline, but some of the 
	/// 	settings will only have an effect in the fixed-function pipeline (for example, 
	/// 	setting a texture rotation will have no effect if you use the programmable
	/// 	pipeline, because this is overridden by the fragment program). The effect
	/// 	of each setting as regards the 2 pipelines is commented in each setting.
	/// 	<p/>
	/// 	When I use the term 'fixed-function pipeline' I mean traditional rendering
	/// 	where you do not use vertex or fragment programs (shaders). Programmable 
	/// 	pipeline means that for this pass you are using vertex or fragment programs.
	/// </remarks>
	/// TODO: Destroy controllers
	public class TextureUnitState
	{
		#region Fields and Properties

		/// <summary>
		///    Maximum amount of animation frames allowed.
		/// </summary>
		public const int MaxAnimationFrames = 32;

		/// <summary>
		///    The parent Pass that owns this TextureUnitState.
		/// </summary>
		protected Pass parent;

		///		Gets the number of frames for a texture.
		/// <summary>
		///    Gets a reference to the Pass that owns this TextureUnitState.
		/// </summary>
		public Pass Parent
		{
			get
			{
				return this.parent;
			}

			[OgreVersion( 1, 7, 2, "Original name _notifyParent" )]
			set
			{
				this.parent = value;
			}
		}

		/// <summary>
		///    Returns true if the resource for this texture layer have been loaded.
		/// </summary>
		public bool IsLoaded
		{
			get
			{
				return this.parent.IsLoaded;
			}
		}

		/// <summary>
		///		Gets/Sets the texture coordinate set to be used by this texture layer.
		/// </summary>
		/// <remarks>
		///		Default is 0 for all layers. Only change this if you have provided multiple texture coords per
		///		vertex.
		///		<p/>
		///		Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		public int TextureCoordSet { get; set; }

		/// <summary>
		///    Addressing mode to use for texture coordinates.
		/// </summary>
		protected UVWAddressing texAddressingMode;

		/// <summary>
		/// Gets the texture addressing mode for a given coordinate, 
		///	i.e. what happens at uv values above 1.0.
		/// </summary>
		/// <remarks>
		///    The default is <code>TextureAddressing.Wrap</code> i.e. the texture repeats over values of 1.0.
		///    This applies for both the fixed-function and programmable pipelines.
		/// </remarks>
		public UVWAddressing TextureAddressingMode
		{
			get
			{
				return this.texAddressingMode;
			}
		}

		/// <summary>
		///    Border color to use when texture addressing mode is set to Border
		/// </summary>
		private ColorEx texBorderColor = ColorEx.Black;

		/// <summary>
		///    Gets/Sets the texture border color, which is used to fill outside the 0-1 range of
		///    texture coordinates when the texture addressing mode is set to Border.
		/// </summary>
		public ColorEx TextureBorderColor
		{
			get
			{
				return this.texBorderColor;
			}
			set
			{
				this.texBorderColor = value;
			}
		}

		/// <summary>
		///    Reference to a class containing the color blending operation params for this stage.
		/// </summary>
		private LayerBlendModeEx colorBlendMode = new LayerBlendModeEx();

		/// <summary>
		///		Gets a structure that describes the layer blending mode parameters.
		/// </summary>
		public LayerBlendModeEx ColorBlendMode
		{
			get
			{
				return this.colorBlendMode;
			}
		}

		/// <summary>
		///    Reference to a class containing the alpha blending operation params for this stage.
		/// </summary>
		private LayerBlendModeEx alphaBlendMode = new LayerBlendModeEx();

		/// <summary>
		///		Gets a structure that describes the layer blending mode parameters.
		/// </summary>
		public LayerBlendModeEx AlphaBlendMode
		{
			get
			{
				return this.alphaBlendMode;
			}
		}

		/// <summary>
		///    Fallback source blending mode, for use if the desired mode is not available.
		/// </summary>
		private SceneBlendFactor colorBlendFallbackSrc;

		/// <summary>
		///    Gets/Sets the multipass fallback for color blending operation source factor.
		/// </summary>
		public SceneBlendFactor ColorBlendFallbackSource
		{
			get
			{
				return this.colorBlendFallbackSrc;
			}
		}

		/// <summary>
		///    Fallback destination blending mode, for use if the desired mode is not available.
		/// </summary>
		private SceneBlendFactor colorBlendFallbackDest;

		/// <summary>
		///    Gets/Sets the multipass fallback for color blending operation destination factor.
		/// </summary>
		public SceneBlendFactor ColorBlendFallbackDest
		{
			get
			{
				return this.colorBlendFallbackDest;
			}
		}

		/// <summary>
		///    Operation to use (add, modulate, etc.) for color blending between stages.
		/// </summary>
		private LayerBlendOperation colorOp;

		public LayerBlendOperation ColorOperation
		{
			get
			{
				return this.colorOp;
			}
			set
			{
				SetColorOperation( value );
			}
		}

		/// <summary>
		///    Is this a blank layer (i.e. no textures, or texture failed to load)?
		/// </summary>
		private bool isBlank;

		/// <summary>
		///    Gets/Sets wether this texture layer is currently blank.
		/// </summary>
		public bool IsBlank
		{
			get
			{
				return this.isBlank;
			}
			set
			{
				this.isBlank = value;
			}
		}

		/// <summary>
		///    Is this a series of 6 2D textures to make up a cube?
		/// </summary>
		private bool isCubic;

		/// <summary></summary>
		/// <remarks>
		///    Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		public bool IsCubic
		{
			get
			{
				return this.isCubic;
			}
		}

		/// <summary>
		///    Number of frames for this layer.
		/// </summary>
		private int numFrames;

		/// <summary></summary>
		/// <remarks>
		///    Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		public int NumFrames
		{
			get
			{
				return this.numFrames;
			}
		}

		/// <summary>
		///    Duration (in seconds) of the animated texture (if any).
		/// </summary>
		private Real animDuration;

		/// <summary>
		///    Index of the current frame of animation (always 0 for single texture stages).
		/// </summary>
		private int currentFrame;

		/// <summary>
		///		Gets/Sets the active frame in an animated or multi-image texture.
		/// </summary>
		/// <remarks>
		///		An animated texture (or a cubic texture where the images are not combined for 3D use) is made up of
		///		a number of frames. This method sets the active frame.
		///		<p/>
		///		Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		public int CurrentFrame
		{
			get
			{
				return this.currentFrame;
			}
			set
			{
				Debug.Assert( value < this.numFrames,
				              "Cannot set the current frame of a texture layer to be greater than the number of frames in the layer." );
				this.currentFrame = value;

				// this will affect the passes hashcode because of the texture name change
				this.parent.DirtyHash();
			}
		}

		/// <summary>
		///    Store names of textures for animation frames.
		/// </summary>
		private string[] frames = new string[MaxAnimationFrames];

		/// <summary>
		///     Optional name for the texture unit state
		/// </summary>
		private string name;

		/// <summary>
		///    Get/Set the name of this texture unit state
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
				if ( this.textureNameAlias == null )
				{
					this.textureNameAlias = this.name;
				}
			}
		}

		/// <summary>
		///     Optional alias for texture frames
		/// </summary>
		private string textureNameAlias;

		/// <summary>
		///    Get/Set the alias for this texture unit state.
		/// </summary>
		public string TextureNameAlias
		{
			get
			{
				return this.textureNameAlias;
			}
			set
			{
				this.textureNameAlias = value;
			}
		}

		/// <summary>
		///		Gets/Sets the name of the texture for this texture pass.
		/// </summary>
		/// <remarks>
		///    This will either always be a single name for this layer,
		///    or will be the name of the current frame for an animated
		///    or otherwise multi-frame texture.
		///    <p/>
		///    Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		public string TextureName
		{
			get
			{
				return this.frames[ this.currentFrame ];
			}
		}

		/// <summary>
		///    Flag the determines if a recalc of the texture matrix is required, usually set after a rotate or
		///    other transformations.
		/// </summary>
		private bool recalcTexMatrix;

		private float transU;

		/// <summary>
		///    U coord of the texture transformation.
		/// </summary>
		public float TextureScrollU
		{
			get
			{
				return this.transU;
			}
			set
			{
				SetTextureScrollU( value );
			}
		}

		/// <summary>
		/// Get/ Set the texture pointer for the current frame.
		/// </summary>
		internal Texture Texture
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return GetTexture( this.currentFrame );
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				SetTexture( value, this.currentFrame );
			}
		}

		/// <summary>
		/// Get the texture pointer for a given frame.
		/// </summary>
		internal Texture GetTexture( int frame )
		{
			throw new System.NotImplementedException();
			//if (mContentType == CONTENT_NAMED)
			//{
			//    if (frame < mFrames.size() && !mTextureLoadFailed)
			//    {
			//        ensureLoaded(frame);
			//        return mFramePtrs[frame];
			//    }
			//    else
			//    {
			//        // Silent fail with empty texture for internal method
			//        static TexturePtr nullTexPtr;
			//        return nullTexPtr;
			//    }
			//}
			//else
			//{
			//    // Manually bound texture, no name or loading
			//    assert(frame < mFramePtrs.size());
			//    return mFramePtrs[frame];
			//}
		}

		/// <summary>
		/// Set the texture pointer for a given frame (internal use only!).
		/// </summary>
		internal void SetTexture( Texture texptr, int frame )
		{
			throw new System.NotImplementedException();
			//assert( frame < mFramePtrs.size() );
			//mFramePtrs[ frame ] = texptr;
		}

		private float transV;

		/// <summary>
		///    V coord of the texture transformation.
		/// </summary>
		public float TextureScrollV
		{
			get
			{
				return this.transV;
			}
			set
			{
				SetTextureScrollV( value );
			}
		}

		private readonly float scrollU;

		/// <summary>
		///    U coord of the texture scroll animation
		/// </summary>
		public float TextureAnimU
		{
			get
			{
				return this.scrollU;
			}
			set
			{
				SetScrollAnimation( value, this.scrollV );
			}
		}

		private readonly float scrollV;

		/// <summary>
		///    V coord of the texture scroll animation
		/// </summary>
		public float TextureAnimV
		{
			get
			{
				return this.scrollV;
			}
			set
			{
				SetScrollAnimation( this.scrollU, value );
			}
		}

		private float scaleU;

		/// <summary>
		///    U scale value of the texture transformation.
		/// </summary>
		public float ScaleU
		{
			get
			{
				return this.scaleU;
			}
			set
			{
				SetTextureScaleU( value );
			}
		}


		private float scaleV;

		/// <summary>
		///    V scale value of the texture transformation.
		/// </summary>
		public float ScaleV
		{
			get
			{
				return this.scaleV;
			}
			set
			{
				SetTextureScaleV( value );
			}
		}

		/// <summary>
		///    Rotation value of the texture transformation.
		/// </summary>
		private float rotate;

		/// <summary>
		///    4x4 texture matrix which gets updated based on various transformations made to this stage.
		/// </summary>
		private Matrix4 texMatrix;

		/// <summary>
		///		Gets/Sets the Matrix4 that represents transformation to the texture in this layer.
		/// </summary>
		/// <remarks>
		///    Texture coordinates can be modified on a texture layer to create effects like scrolling
		///    textures. A texture transform can either be applied to a layer which takes the source coordinates
		///    from a fixed set in the geometry, or to one which generates them dynamically (e.g. environment mapping).
		///    <p/>
		///    It's obviously a bit impractical to create scrolling effects by calling this method manually since you
		///    would have to call it every frame with a slight alteration each time, which is tedious. Instead
		///    you can use the ControllerManager class to create a Controller object which will manage the
		///    effect over time for you. See <see cref="ControllerManager.CreateTextureUVScroller"/> and it's sibling methods for details.<BR/>
		///    In addition, if you want to set the individual texture transformations rather than concatenating them
		///    yourself, use <see cref="SetTextureScroll"/>, <see cref="SetTextureScroll"/> and <see cref="SetTextureRotate"/>. 
		///    <p/>
		///    This has no effect in the programmable pipeline.
		/// </remarks>
		/// <seealso cref="Controller&lt;T&gt;"/><seealso cref="ControllerManager"/>
		public Matrix4 TextureMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				// update the matrix before returning it if necessary
				if ( this.recalcTexMatrix )
				{
					RecalcTextureMatrix();
				}
				return this.texMatrix;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				this.texMatrix = value;
				this.recalcTexMatrix = false;
			}
		}

		/// <summary>
		///    List of effects to apply during this texture stage.
		/// </summary>
		private TextureEffectList effectList = new TextureEffectList();

		/// <summary>
		///    Gets the number of effects currently tied to this texture stage.
		/// </summary>
		public int NumEffects
		{
			get
			{
				return this.effectList.Count;
			}
		}

		/// <summary>
		///    Type of texture this is.
		/// </summary>
		private TextureType textureType;

		/// <summary>
		///    Gets the type of texture this unit has.
		/// </summary>
		public TextureType TextureType
		{
			get
			{
				return this.textureType;
			}
		}

		/// <summary>
		/// the desired pixel format when load the texture
		/// </summary>
		private PixelFormat desiredFormat;

		/// <summary>
		/// The desired pixel format when load the texture.
		/// </summary>
		public PixelFormat DesiredFormat
		{
			get
			{
				return this.desiredFormat;
			}
			set
			{
				this.desiredFormat = value;
			}
		}

		/// <summary>
		/// how many mipmaps have been requested for the texture
		/// </summary>
		private int textureSrcMipmaps;

		/// <summary>
		/// How many mipmaps have been requested for the texture.
		/// </summary>
		public int MipmapCount
		{
			get
			{
				return this.textureSrcMipmaps;
			}
			set
			{
				this.textureSrcMipmaps = value;
			}
		}

		/// <summary>
		/// whether this texture is requested to be loaded as alpha if single channel
		/// </summary>
		private bool isAlpha;

		/// <summary>
		/// Whether this texture is requested to be loaded as alpha if single channel.
		/// </summary>
		public bool IsAlpha
		{
			get
			{
				return this.isAlpha;
			}
			set
			{
				this.isAlpha = value;
			}
		}

		/// <summary>
		/// Whether this texture will be set up so that on sampling it, 
		/// hardware gamma correction is applied.
		/// </summary>
		public bool IsHardwareGammaEnabled { get; set; }

		/// <summary>
		///    Texture filtering - minification.
		/// </summary>
		private FilterOptions minFilter;

		/// <summary>
		///    Texture filtering - magnification.
		/// </summary>
		private FilterOptions magFilter;

		/// <summary>
		///    Texture filtering - mipmapping.
		/// </summary>
		private FilterOptions mipFilter;

		/// <summary>
		///    Is the filtering level the default?
		/// </summary>
		private bool isDefaultFiltering;

		/// <summary>
		///     Reference to an animation controller for this texture unit.
		/// </summary>
		private Controller<Real> animController;

		/// <summary>
		///     Reference to the environment mapping type for this texunit.
		/// </summary>
		private EnvironmentMap environMap;

		private bool envMapEnabled = false;

		public bool EnvironmentMapEnabled
		{
			get
			{
				return this.envMapEnabled;
			}
		}

		private float rotationSpeed = 0;

		public float RotationSpeed
		{
			get
			{
				return this.rotationSpeed;
			}
			set
			{
				SetRotateAnimation( value );
			}
		}

		/// <summary>
		///    Anisotropy setting for this stage.
		/// </summary>
		private int maxAnisotropy;

		/// <summary>
		///    Is anisotropy the default?
		/// </summary>
		private bool isDefaultAniso;

		/// <summary>
		///    Gets/Sets the anisotropy level to be used for this texture stage.
		/// </summary>
		/// <remarks>
		///    This option applies in both the fixed function and the programmable pipeline.
		/// </remarks>
		/// <value>
		///    The maximal anisotropy level, should be between 2 and the maximum supported by hardware (1 is the default, ie. no anisotropy)
		/// </value>
		public int TextureAnisotropy
		{
			get
			{
				return this.isDefaultAniso ? MaterialManager.Instance.DefaultAnisotropy : this.maxAnisotropy;
			}
			set
			{
				this.maxAnisotropy = value;
				this.isDefaultAniso = false;
			}
		}

		/// <summary>
		///    Returns true if this texture unit requires an updated view matrix
		///    to allow for proper texture matrix generation.
		/// </summary>
		public bool HasViewRelativeTexCoordGen
		{
			get
			{
				// TODO: Optimize this to hopefully eliminate the search every time
				foreach ( var effect in this.effectList )
				{
					if ( effect.subtype == (System.Enum)EnvironmentMap.Reflection )
					{
						return true;
					}

					if ( effect.type == TextureEffectType.ProjectiveTexture )
					{
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Returns true if this texture layer uses a composit 3D cubic texture.
		/// </summary>
		public bool Is3D
		{
			get
			{
				return this.textureType == TextureType.CubeMap;
			}
		}

		/// <summary>
		/// The type of unit these texture settings should be bound to
		/// </summary>
		/// <remarks>
		/// Some render systems, when implementing vertex texture fetch, separate
		/// the binding of textures for use in the vertex program versus those
		/// used in fragment programs. This setting allows you to target the
		/// vertex processing unit with a texture binding, in those cases. For
		/// rendersystems which have a unified binding for the vertex and fragment
		/// units, this setting makes no difference.
		/// </remarks>
		public TextureBindingType BindingType { get; set; }

		[OgreVersion( 1, 7, 2790 )]
		public float TextureMipmapBias { get; set; }

		#endregion Fields and Properties

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		/// <param name="parent">Parent Pass of this TextureUnitState.</param>
		public TextureUnitState( Pass parent )
			: this( parent, "", 0 )
		{
		}

		/// <summary>
		///		Name based constructor.
		/// </summary>
		/// <param name="parent">Parent Pass of this texture stage.</param>
		/// <param name="textureName">Name of the texture for this texture stage.</param>
		public TextureUnitState( Pass parent, string textureName )
			: this( parent, textureName, 0 )
		{
		}

		/// <summary>
		///		Constructor.
		/// </summary>
		public TextureUnitState( Pass parent, string textureName, int texCoordSet )
		{
			this.parent = parent;
			this.isBlank = true;

			this.colorBlendMode.blendType = LayerBlendType.Color;
			SetColorOperation( LayerBlendOperation.Modulate );
			SetTextureAddressingMode( TextureAddressing.Wrap );

			// set alpha blending options
			this.alphaBlendMode.operation = LayerBlendOperationEx.Modulate;
			this.alphaBlendMode.blendType = LayerBlendType.Alpha;
			this.alphaBlendMode.source1 = LayerBlendSource.Texture;
			this.alphaBlendMode.source2 = LayerBlendSource.Current;

			// default filtering and anisotropy
			this.minFilter = FilterOptions.Linear;
			this.magFilter = FilterOptions.Linear;
			this.mipFilter = FilterOptions.Point;
			this.maxAnisotropy = MaterialManager.Instance.DefaultAnisotropy;
			this.isDefaultFiltering = true;
			this.isDefaultAniso = true;

			// texture modification params
			this.scrollU = this.scrollV = 0;
			this.transU = this.transV = 0;
			this.scaleU = this.scaleV = 1;
			this.rotate = 0;
			this.texMatrix = Matrix4.Identity;
			this.animDuration = 0;

			this.textureType = TextureType.TwoD;

			this.textureSrcMipmaps = (int)TextureMipmap.Default;
			// texture params
			SetTextureName( textureName );
			TextureCoordSet = texCoordSet;

			parent.DirtyHash();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the texture addressing mode, i.e. what happens at uv values above 1.0.
		/// </summary>
		/// <remarks>
		/// The default is TAM_WRAP i.e. the texture repeats over values of 1.0.
		/// This is a shortcut method which sets the addressing mode for all
		///	coordinates at once; you can also call the more specific method
		///	to set the addressing mode per coordinate.
		/// This applies for both the fixed-function and programmable pipelines.
		/// </remarks>
		/// <param name="tam"></param>
		public void SetTextureAddressingMode( TextureAddressing tam )
		{
			this.texAddressingMode = new UVWAddressing( tam );
		}

		/// <summary>
		/// Sets the texture addressing mode, i.e. what happens at uv values above 1.0.
		/// </summary>
		/// <remarks>
		/// The default is TAM_WRAP i.e. the texture repeats over values of 1.0.
		/// This applies for both the fixed-function and programmable pipelines.
		/// </remarks>
		public void SetTextureAddressingMode( TextureAddressing u, TextureAddressing v, TextureAddressing w )
		{
			this.texAddressingMode = new UVWAddressing( u, v, w );
		}

		/// <summary>
		/// Sets the texture addressing mode, i.e. what happens at uv values above 1.0.
		/// </summary>
		/// <remarks>
		/// The default is TAM_WRAP i.e. the texture repeats over values of 1.0.
		/// This applies for both the fixed-function and programmable pipelines.
		/// </remarks>
		/// <param name="uvw"></param>
		public void SetTextureAddressingMode( UVWAddressing uvw )
		{
			this.texAddressingMode = uvw;
		}

		/// <summary>
		///    Enables or disables projective texturing on this texture unit.
		/// </summary>
		/// <remarks>
		///	   <p>
		///	   Projective texturing allows you to generate texture coordinates 
		///	   based on a Frustum, which gives the impression that a texture is
		///	   being projected onto the surface. Note that once you have called
		///	   this method, the texture unit continues to monitor the Frustum you 
		///	   passed in and the projection will change if you can alter it. It also
		///	   means that the Frustum object you pass remains in existence for as long
		///	   as this TextureUnitState does.
		///	   </p>
		///    <p>
		///	   This effect cannot be combined with other texture generation effects, 
		///	   such as environment mapping. It also has no effect on passes which 
		///	   have a vertex program enabled - projective texturing has to be done
		///	   in the vertex program instead.
		///    </p>
		/// </remarks>
		/// <param name="enable">
		///    Whether to enable / disable
		/// </param>
		/// <param name="projectionSettings">
		///    The Frustum which will be used to derive the projection parameters.
		/// </param>
		public void SetProjectiveTexturing( bool enable, Frustum projectionSettings )
		{
			if ( enable )
			{
				var effect = new TextureEffect();
				effect.type = TextureEffectType.ProjectiveTexture;
				effect.frustum = projectionSettings;
				AddEffect( effect );
			}
			else
			{
				RemoveEffect( TextureEffectType.ProjectiveTexture );
			}
		}

		/// <summary>
		///    Gets the texture effect at the specified index.
		/// </summary>
		/// <param name="index">Index of the texture effect to retrieve.</param>
		/// <returns>The TextureEffect at the specified index.</returns>
		public TextureEffect GetEffect( int index )
		{
			Debug.Assert( index < this.effectList.Count, "index < effectList.Count" );

			return (TextureEffect)this.effectList[ index ];
		}

		/// <summary>
		///    Removes all effects from this texture stage.
		/// </summary>
		public void RemoveAllEffects()
		{
			this.effectList.Clear();
		}

		/// <summary>
		///    Removes the specified effect from the list of effects being applied during this
		///    texture stage.
		/// </summary>
		/// <param name="effect">Effect to remove.</param>
		public void RemoveEffect( TextureEffect effect )
		{
			this.effectList.Remove( effect );
		}

		/// <summary>
		///    Sets the multipass fallback operation for this layer, if you used TextureUnitState.SetColorOperationEx
		///    and not enough multitexturing hardware is available.
		/// </summary>
		/// <remarks>
		///    Because some effects exposed using TextureUnitState.SetColorOperationEx are only supported under
		///    multitexturing hardware, if the hardware is lacking the system must fallback on multipass rendering,
		///    which unfortunately doesn't support as many effects. This method is for you to specify the fallback
		///    operation which most suits you.
		///    <p/>
		///    You'll notice that the interface is the same as the Material.SetSceneBlending method; this is
		///    because multipass rendering IS effectively scene blending, since each layer is rendered on top
		///    of the last using the same mechanism as making an object transparent, it's just being rendered
		///    in the same place repeatedly to get the multitexture effect.
		///    <p/>
		///    If you use the simpler (and hence less flexible) TextureUnitState.SetColorOperation method you
		///    don't need to call this as the system sets up the fallback for you.
		///    <p/>
		///    This option has no effect in the programmable pipeline, because there is no multipass fallback
		///    and multitexture blending is handled by the fragment shader.
		/// </remarks>
		/// <param name="src">How to apply the source color during blending.</param>
		/// <param name="dest">How to affect the destination color during blending.</param>
		public void SetColorOpMultipassFallback( SceneBlendFactor src, SceneBlendFactor dest )
		{
			this.colorBlendFallbackSrc = src;
			this.colorBlendFallbackDest = dest;
		}

		/// <summary>
		///    Sets this texture layer to use a combination of 6 texture maps, each one relating to a face of a cube.
		/// </summary>
		/// <remarks>
		///    Cubic textures are made up of 6 separate texture images. Each one of these is an orthoganal view of the
		///    world with a FOV of 90 degrees and an aspect ratio of 1:1. You can generate these from 3D Studio by
		///    rendering a scene to a reflection map of a transparent cube and saving the output files.
		///    <p/>
		///    Cubic maps can be used either for skyboxes (complete wrap-around skies, like space) or as environment
		///    maps to simulate reflections. The system deals with these 2 scenarios in different ways:
		///    <ol>
		///    <li>
		///    <p>
		///    For cubic environment maps, the 6 textures are combined into a single 'cubic' texture map which
		///    is then addressed using 3D texture coordinates. This is required because you don't know what
		///    face of the box you're going to need to address when you render an object, and typically you
		///    need to reflect more than one face on the one object, so all 6 textures are needed to be
		///    'active' at once. Cubic environment maps are enabled by calling this method with the forUVW
		///    parameter set to true, and then calling <code>SetEnvironmentMap(true)</code>.
		///    </p>
		///    <p>
		///    Note that not all cards support cubic environment mapping.
		///    </p>
		///    </li>
		///    <li>
		///    <p>
		///    For skyboxes, the 6 textures are kept separate and used independently for each face of the skybox.
		///    This is done because not all cards support 3D cubic maps and skyboxes do not need to use 3D
		///    texture coordinates so it is simpler to render each face of the box with 2D coordinates, changing
		///    texture between faces.
		///    </p>
		///    <p>
		///    Skyboxes are created by calling SceneManager.SetSkyBox.
		///    </p>
		///    </li>
		///    </ol>
		///    <p/>
		///    Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		/// <param name="textureName">
		///    The basic name of the texture e.g. brickwall.jpg, stonefloor.png. There must be 6 versions
		///    of this texture with the suffixes _fr, _bk, _up, _dn, _lf, and _rt (before the extension) which
		///    make up the 6 sides of the box. The textures must all be the same size and be powers of 2 in width &amp; height.
		///    If you can't make your texture names conform to this, use the alternative method of the same name which takes
		///    an array of texture names instead.
		/// </param>
		/// <param name="forUVW">
		///    Set to true if you want a single 3D texture addressable with 3D texture coordinates rather than
		///    6 separate textures. Useful for cubic environment mapping.
		/// </param>
		public void SetCubicTextureName( string textureName, bool forUVW )
		{
			if ( forUVW )
			{
				// pass in the single texture name
				SetCubicTextureName( new string[]
				                     {
				                     	textureName
				                     }, forUVW );
			}
			else
			{
				string[] postfixes = {
				                     	"_fr", "_bk", "_lf", "_rt", "_up", "_dn"
				                     };
				var fullNames = new string[6];
				string baseName;
				string ext;

				var pos = textureName.LastIndexOf( "." );

				baseName = textureName.Substring( 0, pos );
				ext = textureName.Substring( pos );

				for ( var i = 0; i < 6; i++ )
				{
					fullNames[ i ] = baseName + postfixes[ i ] + ext;
				}

				SetCubicTextureName( fullNames, forUVW );
			}
		}

		/// <summary>
		///    Sets this texture layer to use a combination of 6 texture maps, each one relating to a face of a cube.
		/// </summary>
		/// <remarks>
		///    Cubic textures are made up of 6 separate texture images. Each one of these is an orthoganal view of the
		///    world with a FOV of 90 degrees and an aspect ratio of 1:1. You can generate these from 3D Studio by
		///    rendering a scene to a reflection map of a transparent cube and saving the output files.
		///    <p/>
		///    Cubic maps can be used either for skyboxes (complete wrap-around skies, like space) or as environment
		///    maps to simulate reflections. The system deals with these 2 scenarios in different ways:
		///    <ul>
		///    <li>
		///    <p>
		///    For cubic environment maps, the 6 textures are combined into a single 'cubic' texture map which
		///    is then addressed using 3D texture coordinates. This is required because you don't know what
		///    face of the box you're going to need to address when you render an object, and typically you
		///    need to reflect more than one face on the one object, so all 6 textures are needed to be
		///    'active' at once. Cubic environment maps are enabled by calling this method with the forUVW
		///    parameter set to true, and then calling <code>SetEnvironmentMap(true)</code>.
		///    </p>
		///    <p>
		///    Note that not all cards support cubic environment mapping.
		///    </p>
		///    </li>
		///    <li>
		///    <p>
		///    For skyboxes, the 6 textures are kept separate and used independently for each face of the skybox.
		///    This is done because not all cards support 3D cubic maps and skyboxes do not need to use 3D
		///    texture coordinates so it is simpler to render each face of the box with 2D coordinates, changing
		///    texture between faces.
		///    </p>
		///    <p>
		///    Skyboxes are created by calling SceneManager.SetSkyBox.
		///    </p>
		///    </li>
		///    </ul>
		///    <p/>
		///    Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		/// <param name="textureNames">
		///    6 versions of this texture with the suffixes _fr, _bk, _up, _dn, _lf, and _rt (before the extension) which
		///    make up the 6 sides of the box. The textures must all be the same size and be powers of 2 in width &amp; height.
		///    If you can't make your texture names conform to this, use the alternative method of the same name which takes
		///    an array of texture names instead.
		/// </param>
		/// <param name="forUVW">
		///    Set to true if you want a single 3D texture addressable with 3D texture coordinates rather than
		///    6 separate textures. Useful for cubic environment mapping.
		/// </param>
		public void SetCubicTextureName( string[] textureNames, bool forUVW )
		{
			this.numFrames = forUVW ? 1 : 6;
			this.currentFrame = 0;
			this.isCubic = true;
			this.textureType = forUVW ? TextureType.CubeMap : TextureType.TwoD;

			for ( var i = 0; i < this.numFrames; i++ )
			{
				this.frames[ i ] = textureNames[ i ];
			}

			// tell parent we need recompiling, will cause reload too
			this.parent.NotifyNeedsRecompile();
		}

		/// <summary>
		///		Determines how this texture layer is combined with the one below it (or the diffuse color of
		///		the geometry if this is layer 0).
		/// </summary>
		/// <remarks>
		///    This method is the simplest way to blend tetxure layers, because it requires only one parameter,
		///    gives you the most common blending types, and automatically sets up 2 blending methods: one for
		///    if single-pass multitexturing hardware is available, and another for if it is not and the blending must
		///    be achieved through multiple rendering passes. It is, however, quite limited and does not expose
		///    the more flexible multitexturing operations, simply because these can't be automatically supported in
		///    multipass fallback mode. If want to use the fancier options, use 
		///    <see cref="TextureUnitState.SetColorOperationEx(LayerBlendOperationEx, LayerBlendSource, LayerBlendSource,
		///    ColorEx, ColorEx, float)"/>,
		///    but you'll either have to be sure that enough multitexturing units will be available, or you should
		///    explicitly set a fallback using <see cref="TextureUnitState.SetColorOpMultipassFallback"/>.
		///    <p/>
		///    The default method is LayerBlendOperation.Modulate for all layers.
		///    <p/>
		///    This option has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="operation">One of the LayerBlendOperation enumerated blending types.</param>
		public void SetColorOperation( LayerBlendOperation operation )
		{
			this.colorOp = operation;

			// configure the multitexturing operations
			switch ( operation )
			{
				case LayerBlendOperation.Replace:
					SetColorOperationEx( LayerBlendOperationEx.Source1, LayerBlendSource.Texture, LayerBlendSource.Current );
					SetColorOpMultipassFallback( SceneBlendFactor.One, SceneBlendFactor.Zero );
					break;

				case LayerBlendOperation.Add:
					SetColorOperationEx( LayerBlendOperationEx.Add, LayerBlendSource.Texture, LayerBlendSource.Current );
					SetColorOpMultipassFallback( SceneBlendFactor.One, SceneBlendFactor.One );
					break;

				case LayerBlendOperation.Modulate:
					SetColorOperationEx( LayerBlendOperationEx.Modulate, LayerBlendSource.Texture, LayerBlendSource.Current );
					SetColorOpMultipassFallback( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
					break;

				case LayerBlendOperation.AlphaBlend:
					SetColorOperationEx( LayerBlendOperationEx.BlendTextureAlpha, LayerBlendSource.Texture, LayerBlendSource.Current );
					SetColorOpMultipassFallback( SceneBlendFactor.SourceAlpha, SceneBlendFactor.OneMinusSourceAlpha );
					break;
			}
		}

		/// <summary>
		///    For setting advanced blending options.
		/// </summary>
		/// <remarks>
		///    This is an extended version of the <see cref="TextureUnitState.SetColorOperation"/> method which allows
		///    extremely detailed control over the blending applied between this and earlier layers.
		///    See the IMPORTANT note below about the issues between mulitpass and multitexturing that
		///    using this method can create.
		///    <p/>
		///    Texture color operations determine how the final color of the surface appears when
		///    rendered. Texture units are used to combine color values from various sources (ie. the
		///    diffuse color of the surface from lighting calculations, combined with the color of
		///    the texture). This method allows you to specify the 'operation' to be used, ie. the
		///    calculation such as adds or multiplies, and which values to use as arguments, such as
		///    a fixed value or a value from a previous calculation.
		///    <p/>
		///    The defaults for each layer are:
		///    <ul>
		///    <li>op = Modulate</li>
		///    <li>source1 = Texture</li>
		///    <li>source2 = Current</li>
		///    </ul>
		///    ie. each layer takes the color results of the previous layer, and multiplies them
		///    with the new texture being applied. Bear in mind that colors are RGB values from
		///    0.0 - 1.0 so multiplying them together will result in values in the same range,
		///    'tinted' by the multiply. Note however that a straight multiply normally has the
		///    effect of darkening the textures - for this reason there are brightening operations
		///    like ModulateX2. See the LayerBlendOperation and LayerBlendSource enumerated
		///    types for full details.
		///    <p/>
		///    Because of the limitations on some underlying APIs (Direct3D included)
		///    the Texture argument can only be used as the first argument, not the second.
		///    <p/>
		///    The final 3 parameters are only required if you decide to pass values manually
		///    into the operation, i.e. you want one or more of the inputs to the color calculation
		///    to come from a fixed value that you supply. Hence you only need to fill these in if
		///    you supply <code>Manual</code> to the corresponding source, or use the 
		///    <code>BlendManual</code> operation.
		///    <p/>
		///    The engine tries to use multitexturing hardware to blend texture layers
		///    together. However, if it runs out of texturing units (e.g. 2 of a GeForce2, 4 on a
		///    GeForce3) it has to fall back on multipass rendering, i.e. rendering the same object
		///    multiple times with different textures. This is both less efficient and there is a smaller
		///    range of blending operations which can be performed. For this reason, if you use this method
		///    you MUST also call <see cref="TextureUnitState.SetColorOpMultipassFallback"/> to specify which effect you
		///    want to fall back on if sufficient hardware is not available.
		///    <p/>
		///    If you wish to avoid having to do this, use the simpler <see cref="TextureUnitState.SetColorOperation"/> method
		///    which allows less flexible blending options but sets up the multipass fallback automatically,
		///    since it only allows operations which have direct multipass equivalents.
		///    <p/>
		///    This has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
		/// <param name="source1">The source of the first color to the operation e.g. texture color.</param>
		/// <param name="source2">The source of the second color to the operation e.g. current surface color.</param>
		/// <param name="arg1">Manually supplied color value (only required if source1 = Manual).</param>
		/// <param name="arg2">Manually supplied color value (only required if source2 = Manual)</param>
		/// <param name="blendFactor">
		///    Manually supplied 'blend' value - only required for operations
		///    which require manual blend e.g. LayerBlendOperationEx.BlendManual
		/// </param>
		public void SetColorOperationEx( LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2,
		                                 ColorEx arg1, ColorEx arg2, float blendFactor )
		{
			this.colorBlendMode.operation = operation;
			this.colorBlendMode.source1 = source1;
			this.colorBlendMode.source2 = source2;
			this.colorBlendMode.colorArg1 = arg1;
			this.colorBlendMode.colorArg2 = arg2;
			this.colorBlendMode.blendFactor = blendFactor;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
		public void SetColorOperationEx( LayerBlendOperationEx operation )
		{
			SetColorOperationEx( operation, LayerBlendSource.Texture, LayerBlendSource.Current, ColorEx.White, ColorEx.White,
			                     0.0f );
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
		/// <param name="source1">The source of the first color to the operation e.g. texture color.</param>
		/// <param name="source2">The source of the second color to the operation e.g. current surface color.</param>
		public void SetColorOperationEx( LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2 )
		{
			SetColorOperationEx( operation, source1, source2, ColorEx.White, ColorEx.White, 0.0f );
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
		/// <param name="source1">The source of the first color to the operation e.g. texture color.</param>
		/// <param name="source2">The source of the second color to the operation e.g. current surface color.</param>
		/// <param name="arg1">Manually supplied color value (only required if source1 = Manual).</param>		
		public void SetColorOperationEx( LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2,
		                                 ColorEx arg1 )
		{
			SetColorOperationEx( operation, source1, source2, arg1, ColorEx.White, 0.0f );
		}

		/// <summary>
		///    Sets the alpha operation to be applied to this texture.
		/// </summary>
		/// <remarks>
		///    This works in exactly the same way as SetColorOperation, except
		///    that the effect is applied to the level of alpha (i.e. transparency)
		///    of the texture rather than its color. When the alpha of a texel (a pixel
		///    on a texture) is 1.0, it is opaque, wheras it is fully transparent if the
		///    alpha is 0.0. Please refer to the SetColorOperation method for more info.
		/// </remarks>
		/// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
		/// <param name="source1">The source of the first alpha value to the operation e.g. texture alpha.</param>
		/// <param name="source2">The source of the second alpha value to the operation e.g. current surface alpha.</param>
		/// <param name="arg1">Manually supplied alpha value (only required if source1 = LayerBlendSource.Manual).</param>
		/// <param name="arg2">Manually supplied alpha value (only required if source2 = LayerBlendSource.Manual).</param>
		/// <param name="blendFactor">Manually supplied 'blend' value - only required for operations
		///    which require manual blend e.g. LayerBlendOperationEx.BlendManual.
		/// </param>
		public void SetAlphaOperation( LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2,
		                               Real arg1, Real arg2, Real blendFactor )
		{
			this.alphaBlendMode.operation = operation;
			this.alphaBlendMode.source1 = source1;
			this.alphaBlendMode.source2 = source2;
			this.alphaBlendMode.alphaArg1 = arg1;
			this.alphaBlendMode.alphaArg2 = arg2;
			this.alphaBlendMode.blendFactor = blendFactor;
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
		public void SetAlphaOperation( LayerBlendOperationEx operation )
		{
			SetAlphaOperation( operation, LayerBlendSource.Texture, LayerBlendSource.Current, 1.0f, 1.0f, 0.0f );
		}

		public EnvironmentMap GetEnvironmentMap()
		{
			return this.environMap;
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="enable"></param>
		public void SetEnvironmentMap( bool enable )
		{
			// call with Curved as the default value
			SetEnvironmentMap( enable, EnvironmentMap.Curved );
		}

		/// <summary>
		///    Turns on/off texture coordinate effect that makes this layer an environment map.
		/// </summary>
		/// <remarks>
		///    Environment maps make an object look reflective by using the object's vertex normals relative
		///    to the camera view to generate texture coordinates.
		///    <p/>
		///    The vectors generated can either be used to address a single 2D texture which
		///    is a 'fish-eye' lens view of a scene, or a 3D cubic environment map which requires 6 textures
		///    for each side of the inside of a cube. The type depends on what texture you set up - if you use the
		///    setTextureName method then a 2D fisheye lens texture is required, whereas if you used setCubicTextureName
		///    then a cubic environemnt map will be used.
		///    <p/>
		///    This effect works best if the object has lots of gradually changing normals. The texture also
		///    has to be designed for this effect - see the example spheremap.png included with the sample
		///    application for a 2D environment map; a cubic map can be generated by rendering 6 views of a
		///    scene to each of the cube faces with orthoganal views.
		///    <p/>
		///    Enabling this disables any other texture coordinate generation effects.
		///    However it can be combined with texture coordinate modification functions, which then operate on the
		///    generated coordinates rather than static model texture coordinates.
		///    <p/>
		///    This option has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="enable">True to enable, false to disable.</param>
		/// <param name="envMap">
		///    If set to true, instead of being based on normals the environment effect is based on
		///    vertex positions. This is good for planar surfaces.
		/// </param>
		public void SetEnvironmentMap( bool enable, EnvironmentMap envMap )
		{
			this.environMap = envMap;
			this.envMapEnabled = enable;
			if ( enable )
			{
				var effect = new TextureEffect();
				effect.type = TextureEffectType.EnvironmentMap;
				effect.subtype = envMap;
				AddEffect( effect );
			}
			else
			{
				// remove it from the list
				RemoveEffect( TextureEffectType.EnvironmentMap );
			}
		}

		/// <summary>
		///    Gets the name of the texture associated with a frame.
		/// </summary>
		/// <remarks>
		///    Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		/// <param name="frame">Index of the frame to retreive the texture name for.</param>
		/// <returns>The name of the texture at the specified frame index.</returns>
		public string GetFrameTextureName( int frame )
		{
			Debug.Assert( frame < this.numFrames, "Attempted to access a frame which is out of range." );

			return this.frames[ frame ];
		}

		/// <summary>
		///    Gets the texture filtering for the given type.
		/// </summary>
		/// <param name="type">Type of filtering options to retreive.</param>
		/// <returns></returns>
		public FilterOptions GetTextureFiltering( FilterType type )
		{
			switch ( type )
			{
				case FilterType.Min:
					return this.isDefaultFiltering
					       	? MaterialManager.Instance.GetDefaultTextureFiltering( FilterType.Min )
					       	: this.minFilter;

				case FilterType.Mag:
					return this.isDefaultFiltering
					       	? MaterialManager.Instance.GetDefaultTextureFiltering( FilterType.Mag )
					       	: this.magFilter;

				case FilterType.Mip:
					return this.isDefaultFiltering
					       	? MaterialManager.Instance.GetDefaultTextureFiltering( FilterType.Mip )
					       	: this.mipFilter;
			}

			// should never get here, but makes the compiler happy
			return FilterOptions.None;
		}

		/// <summary>
		///     Sets the names of the texture images for an animated texture.
		/// </summary>
		/// <remarks>
		///     Animated textures are just a series of images making up the frames of the animation. All the images
		///     must be the same size, and their names must have a frame number appended before the extension, e.g.
		///     if you specify a name of "wall.jpg" with 3 frames, the image names must be "wall_1.jpg" and "wall_2.jpg".
		///     <p/>
		///     You can change the active frame on a texture layer by setting the CurrentFrame property.
		///     <p/>
		///     Note: If you can't make your texture images conform to the naming standard layed out here, you
		///     can call the alternative SetAnimatedTextureName method which takes an array of names instead.
		/// </remarks>
		/// <param name="name">The base name of the series of textures to use.</param>
		/// <param name="numFrames">Number of frames to be used for this animation.</param>
		/// <param name="duration">
		///     Total length of the animation sequence.  When set to 0, automatic animation does not occur.
		///     In that scenario, the values can be changed manually by setting the CurrentFrame property.
		/// </param>
		public void SetAnimatedTextureName( string name, int numFrames, float duration )
		{
			string ext, baseName;

			// split up the base name and file extension
			var pos = name.LastIndexOf( "." );
			baseName = name.Substring( 0, pos );
			ext = name.Substring( pos );

			var names = new string[numFrames];

			// loop through and create the real texture names from the base name
			for ( var i = 0; i < numFrames; i++ )
			{
				names[ i ] = string.Format( "{0}_{1}{2}", baseName, i, ext );
			}

			SetAnimatedTextureName( names, numFrames, duration );
		}

		/// <summary>
		///     Sets the names of the texture images for an animated texture.
		/// </summary>
		/// <remarks>
		///     Animated textures are just a series of images making up the frames of the animation. All the images
		///     must be the same size, and their names must have a frame number appended before the extension, e.g.
		///     if you specify a name of "wall.jpg" with 3 frames, the image names must be "wall_1.jpg" and "wall_2.jpg".
		///     <p/>
		///     You can change the active frame on a texture layer by setting the CurrentFrame property.
		/// </remarks>
		/// <param name="names">An array containing the array names to use for the animation.</param>
		/// <param name="numFrames">Number of frames to be used for this animation.</param>
		/// <param name="duration">
		///     Total length of the animation sequence.  When set to 0, automatic animation does not occur.
		///     In that scenario, the values can be changed manually by setting the CurrentFrame property.
		/// </param>
		public void SetAnimatedTextureName( string[] names, int numFrames, float duration )
		{
			if ( numFrames > MaxAnimationFrames )
			{
				throw new AxiomException( "Maximum number of texture animation frames exceeded!" );
			}

			this.numFrames = numFrames;
			this.animDuration = duration;
			this.currentFrame = 0;
			this.isCubic = false;

			// copy the texture names
			Array.Copy( names, 0, this.frames, 0, numFrames );

			// if material is already loaded, load this immediately
			if ( IsLoaded )
			{
				Load();

				// tell parent to recalculate the hash
				this.parent.DirtyHash();
			}
		}

		/// <summary>
		///    Sets the translation offset of the texture, ie scrolls the texture.
		/// </summary>
		/// <remarks>
		///    This method sets the translation element of the texture transformation, and is easier to use than setTextureTransform if
		///    you are combining translation, scaling and rotation in your texture transformation. Again if you want
		///    to animate these values you need to use a Controller
		///    <p/>
		///    Has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="u">The amount the texture should be moved horizontally (u direction).</param>
		/// <param name="v">The amount the texture should be moved vertically (v direction).</param>
		public void SetTextureScroll( float u, float v )
		{
			this.transU = u;
			this.transV = v;
			this.recalcTexMatrix = true;
		}

		/// <summary>
		///    Same as in SetTextureScroll, but sets only U value.
		/// </summary>
		/// <remarks>
		///    Has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="u">The amount the texture should be moved horizontally (u direction).</param>
		public void SetTextureScrollU( float u )
		{
			this.transU = u;
			this.recalcTexMatrix = true;
		}

		/// <summary>
		///    Same as in SetTextureScroll, but sets only V value.
		/// </summary>
		/// <remarks>
		///    Has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="v">The amount the texture should be moved vertically (v direction).</param>
		public void SetTextureScrollV( float v )
		{
			this.transV = v;
			this.recalcTexMatrix = true;
		}

		/// <summary>
		///		Sets up an animated scroll for the texture layer.
		/// </summary>
		/// <remarks>
		///    Useful for creating constant scrolling effects on a texture layer (for varying scrolls, <see cref="SetTransformAnimation"/>).
		///    <p/>
		///    This option has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="uSpeed">The number of horizontal loops per second (+ve=moving right, -ve = moving left).</param>
		/// <param name="vSpeed">The number of vertical loops per second (+ve=moving up, -ve= moving down).</param>
		public void SetScrollAnimation( float uSpeed, float vSpeed )
		{
			RemoveEffect( TextureEffectType.UVScroll );
			RemoveEffect( TextureEffectType.UScroll );
			RemoveEffect( TextureEffectType.VScroll );

			// don't create an effect if both Speeds are 0
			if ( uSpeed == 0 && vSpeed == 0 )
			{
				return;
			}

			// Create new effect
			TextureEffect effect;
			if ( uSpeed == vSpeed )
			{
				effect = new TextureEffect();
				effect.type = TextureEffectType.UVScroll;
				effect.arg1 = uSpeed;
				AddEffect( effect );
			}
			else
			{
				if ( uSpeed != 0 )
				{
					effect = new TextureEffect();
					effect.type = TextureEffectType.UScroll;
					effect.arg1 = uSpeed;
					AddEffect( effect );
				}
				if ( vSpeed != 0 )
				{
					effect = new TextureEffect();
					effect.type = TextureEffectType.VScroll;
					effect.arg1 = vSpeed;
					AddEffect( effect );
				}
			}
		}

		/// <summary>
		///		Sets up an animated texture rotation for this layer.
		/// </summary>
		/// <remarks>
		///    Useful for constant rotations (for varying rotations, <see cref="SetTransformAnimation"/>).
		///    <p/>
		///    This option has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="speed">The number of complete counter-clockwise revolutions per second (use -ve for clockwise)</param>
		public void SetRotateAnimation( float speed )
		{
			this.rotationSpeed = speed;
			var effect = new TextureEffect();
			effect.type = TextureEffectType.Rotate;
			effect.arg1 = speed;

			AddEffect( effect );
		}

		/// <summary>
		///    Sets up a general time-relative texture modification effect.
		/// </summary>
		/// <remarks>
		///    This can be called multiple times for different values of <paramref name="transType"/>, but only the latest effect
		///    applies if called multiple time for the same <paramref name="transType"/>.
		///    <p/>
		///    This option has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="transType">The type of transform, either translate (scroll), scale (stretch) or rotate (spin).</param>
		/// <param name="waveType">The shape of the wave, see <see cref="WaveformType"/> enum for details</param>
		/// <param name="baseVal">The base value for the function (range of output = {base, base + amplitude}).</param>
		/// <param name="frequency">The speed of the wave in cycles per second.</param>
		/// <param name="phase">The offset of the start of the wave, e.g. 0.5 to start half-way through the wave.</param>
		/// <param name="amplitude">Scales the output so that instead of lying within [0..1] it lies within [0..(1 * amplitude)] for exaggerated effects.</param>
		public void SetTransformAnimation( TextureTransform transType, WaveformType waveType, float baseVal, float frequency,
		                                   float phase, float amplitude )
		{
			var effect = new TextureEffect();
			effect.type = TextureEffectType.Transform;
			effect.subtype = transType;
			effect.waveType = waveType;
			effect.baseVal = baseVal;
			effect.frequency = frequency;
			effect.phase = phase;
			effect.amplitude = amplitude;

			AddEffect( effect );
		}

		/// <summary>
		///    Sets the scaling factor of the texture.
		/// </summary>
		/// <remarks>
		///    This method sets the scale element of the texture transformation, and is easier to use than
		///    setTextureTransform if you are combining translation, scaling and rotation in your texture transformation. Again if you want
		///    to animate these values you need to use a Controller (see ControllerManager and it's methods for
		///    more information).
		///    <p/>
		///    Has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="u">The value by which the texture is to be scaled horizontally.</param>
		/// <param name="v">The value by which the texture is to be scaled vertically.</param>
		public void SetTextureScale( float u, float v )
		{
			this.scaleU = u;
			this.scaleV = v;
			this.recalcTexMatrix = true;
		}

		/// <summary>
		///    Same as in SetTextureScale, but sets only U value.
		/// </summary>
		/// <remarks>
		///    Has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="u">The value by which the texture is to be scaled horizontally.</param>
		public void SetTextureScaleU( float u )
		{
			this.scaleU = u;
			this.recalcTexMatrix = true;
		}

		/// <summary>
		///    Same as in SetTextureScale, but sets only V value.
		/// </summary>
		/// <remarks>
		///    Has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="v">The value by which the texture is to be scaled vertically.</param>
		public void SetTextureScaleV( float v )
		{
			this.scaleV = v;
			this.recalcTexMatrix = true;
		}

		/// <summary>
		///    Set the texture filtering for this unit, using the simplified interface.
		/// </summary>
		/// <remarks>
		///    You also have the option of specifying the minification, magnification 
		///    and mip filter individually if you want more control over filtering 
		///    options. See the SetTextureFiltering overloads for details. 
		///    <p/>
		///    Note: This option applies in both the fixed function and programmable pipeline.
		/// </remarks>
		/// <param name="filter">
		///    The high-level filter type to use.
		/// </param>
		public void SetTextureFiltering( TextureFiltering filter )
		{
			switch ( filter )
			{
				case TextureFiltering.None:
					SetTextureFiltering( FilterOptions.Point, FilterOptions.Point, FilterOptions.None );
					break;

				case TextureFiltering.Bilinear:
					SetTextureFiltering( FilterOptions.Linear, FilterOptions.Linear, FilterOptions.Point );
					break;

				case TextureFiltering.Trilinear:
					SetTextureFiltering( FilterOptions.Linear, FilterOptions.Linear, FilterOptions.Linear );
					break;

				case TextureFiltering.Anisotropic:
					SetTextureFiltering( FilterOptions.Anisotropic, FilterOptions.Anisotropic, FilterOptions.Linear );
					break;
			}

			// no longer set to current default
			this.isDefaultFiltering = false;
		}

		/// <summary>
		///    Set a single filtering option on this texture unit.
		/// </summary>
		/// <param name="type">
		///    The filtering type to set.
		/// </param>
		/// <param name="options">
		///    The filtering options to set.
		/// </param>
		public void SetTextureFiltering( FilterType type, FilterOptions options )
		{
			switch ( type )
			{
				case FilterType.Min:
					this.minFilter = options;
					break;

				case FilterType.Mag:
					this.magFilter = options;
					break;

				case FilterType.Mip:
					this.mipFilter = options;
					break;
			}

			// no longer set to current default
			this.isDefaultFiltering = false;
		}

		/// <summary>
		///    Set a the detailed filtering options on this texture unit.
		/// </summary>
		/// <param name="minFilter">
		///    The filtering to use when reducing the size of the texture. Can be Point, Linear or Anisotropic.
		/// </param>
		/// <param name="magFilter">
		///    The filtering to use when increasing the size of the texture. Can be Point, Linear or Anisotropic.
		/// </param>
		/// <param name="mipFilter">
		///    The filtering to use between mipmap levels. Can be None (no mipmap), Point or Linear (trilinear).
		/// </param>
		public void SetTextureFiltering( FilterOptions minFilter, FilterOptions magFilter, FilterOptions mipFilter )
		{
			SetTextureFiltering( FilterType.Min, minFilter );
			SetTextureFiltering( FilterType.Mag, magFilter );
			SetTextureFiltering( FilterType.Mip, mipFilter );

			// no longer set to current default
			this.isDefaultFiltering = false;
		}

		/// <summary>
		///    Sets this texture layer to use a single texture, given the name of the texture to use on this layer.
		/// </summary>
		/// <remarks>
		///    Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		/// <param name="name">Name of the texture.</param>
		/// <param name="type">Type of texture this is.</param>
		/// <param name="mipmaps"></param>
		/// <param name="alpha"></param>
		public void SetTextureName( string name, TextureType type, int mipmaps, bool alpha )
		{
			if ( type == TextureType.CubeMap )
			{
				// delegate to cube texture implementation
				SetCubicTextureName( name, true );
			}
			else
			{
				this.frames[ 0 ] = name;
				this.numFrames = 1;
				this.currentFrame = 0;
				this.isCubic = false;
				this.textureType = type;
				this.textureSrcMipmaps = mipmaps;
				this.isAlpha = alpha;

				if ( name.Length == 0 )
				{
					this.isBlank = true;
					return;
				}

				if ( IsLoaded )
				{
					Load(); // reload
				}
				// Tell parent to recalculate hash (for sorting)
				this.parent.DirtyHash();
			}
		}

		/// <summary>
		///    Sets this texture layer to use a single texture, given the name of the texture to use on this layer.
		/// </summary>
		/// <remarks>
		///    Applies to both fixed-function and programmable pipeline.
		/// </remarks>
		/// <param name="name">Name of the texture.</param>
		public void SetTextureName( string name )
		{
			SetTextureName( name, TextureType.TwoD );
		}

		public void SetTextureName( string name, TextureType type )
		{
			SetTextureName( name, type, -1 );
		}

		/// <summary>
		/// </summary>
		public void SetTextureName( string name, TextureType type, int mipmaps )
		{
			SetTextureName( name, type, mipmaps, false );
		}

		/// <summary>
		///    Sets the counter-clockwise rotation factor applied to texture coordinates.
		/// </summary>
		/// <remarks>
		///    This sets a fixed rotation angle - if you wish to animate this, see the
		///    <see cref="ControllerManager.CreateTextureRotator"/> method.
		///    <p/>
		///    Has no effect in the programmable pipeline.
		/// </remarks>
		/// <param name="degrees">The angle of rotation in degrees (counter-clockwise).</param>
		public void SetTextureRotate( float degrees )
		{
			this.rotate = degrees;
			this.recalcTexMatrix = true;
		}

		/// <summary>
		///		Used to update the texture matrix if need be.
		/// </summary>
		private void RecalcTextureMatrix()
		{
			var xform = Matrix4.Identity;

			// texture scaling
			if ( this.scaleU != 1 || this.scaleV != 1 )
			{
				// offset to the center of the texture
				xform.m00 = 1/this.scaleU;
				xform.m11 = 1/this.scaleV;

				// skip matrix mult since first matrix update
				xform.m03 = ( -0.5f*xform.m00 ) + 0.5f;
				xform.m13 = ( -0.5f*xform.m11 ) + 0.5f;
			}

			// texture translation
			if ( this.transU != 0 || this.transV != 0 )
			{
				var xlate = Matrix4.Identity;

				xlate.m03 = this.transU;
				xlate.m13 = this.transV;

				// multiplt the transform by the translation
				xform = xlate*xform;
			}

			if ( this.rotate != 0.0f )
			{
				var rotation = Matrix4.Identity;

				float theta = Utility.DegreesToRadians( this.rotate );
				float cosTheta = Utility.Cos( theta );
				float sinTheta = Utility.Sin( theta );

				// set the rotation portion of the matrix
				rotation.m00 = cosTheta;
				rotation.m01 = -sinTheta;
				rotation.m10 = sinTheta;
				rotation.m11 = cosTheta;

				// offset the center of rotation to the center of the texture
				rotation.m03 = 0.5f + ( ( -0.5f*cosTheta ) - ( -0.5f*sinTheta ) );
				rotation.m13 = 0.5f + ( ( -0.5f*sinTheta ) + ( -0.5f*cosTheta ) );

				// multiply the rotation and transformation matrices
				xform = rotation*xform;
			}

			// store the transformation into the local texture matrix
			this.texMatrix = xform;

			this.recalcTexMatrix = false;
		}

		/// <summary>
		///		Generic method for setting up texture effects.
		/// </summary>
		/// <remarks>
		///    Allows you to specify effects directly by using the TextureEffectType enumeration. The
		///    arguments that go with it depend on the effect type. Only one effect of
		///    each type can be applied to a texture layer.
		///    <p/>
		///    This method is used internally, but it is better generally for applications to use the
		///    more intuitive specialized methods such as SetEnvironmentMap and SetScroll.
		/// </remarks>
		/// <param name="effect"></param>
		public void AddEffect( TextureEffect effect )
		{
			effect.controller = null;

			// these effects must be unique, so remove any existing
			if ( effect.type == TextureEffectType.EnvironmentMap || effect.type == TextureEffectType.UVScroll ||
			     effect.type == TextureEffectType.UScroll || effect.type == TextureEffectType.VScroll ||
			     effect.type == TextureEffectType.Rotate || effect.type == TextureEffectType.ProjectiveTexture )
			{
				for ( var i = 0; i < this.effectList.Count; i++ )
				{
					if ( ( (TextureEffect)this.effectList[ i ] ).type == effect.type )
					{
						this.effectList.RemoveAt( i );
						break;
					}
				} // for
			}

			// create controller
			if ( IsLoaded )
			{
				CreateEffectController( effect );
			}

			// add to internal list
			this.effectList.Add( effect );
		}

		/// <summary>
		///		Removes effects of the specified type from this layers effect list.
		/// </summary>
		/// <param name="type"></param>
		private void RemoveEffect( TextureEffectType type )
		{
			// TODO: Verify this works correctly since we are removing items during a loop
			for ( var i = 0; i < this.effectList.Count; i++ )
			{
				if ( ( (TextureEffect)this.effectList[ i ] ).type == type )
				{
					this.effectList.RemoveAt( i );
					i--;
				}
			}
		}

		/// <summary>
		///     Creates an animation controller if needed for this texture unit.
		/// </summary>
		private void CreateAnimationController()
		{
			this.animController = ControllerManager.Instance.CreateTextureAnimator( this, this.animDuration );
		}

		/// <summary>
		///		Used internally to create a new controller for this layer given the requested effect.
		/// </summary>
		/// <param name="effect"></param>
		private void CreateEffectController( TextureEffect effect )
		{
			// get a reference to the singleton controller manager
			var cMgr = ControllerManager.Instance;

			// create an appropriate controller based on the specified animation
			switch ( effect.type )
			{
				case TextureEffectType.UVScroll:
					effect.controller = cMgr.CreateTextureUVScroller( this, effect.arg1 );
					break;

				case TextureEffectType.UScroll:
					effect.controller = cMgr.CreateTextureUScroller( this, effect.arg1 );
					break;

				case TextureEffectType.VScroll:
					effect.controller = cMgr.CreateTextureVScroller( this, effect.arg1 );
					break;

				case TextureEffectType.Rotate:
					effect.controller = cMgr.CreateTextureRotator( this, effect.arg1 );
					break;

				case TextureEffectType.Transform:
					effect.controller = cMgr.CreateTextureWaveTransformer( this, (TextureTransform)effect.subtype, effect.waveType,
					                                                       effect.baseVal, effect.frequency, effect.phase,
					                                                       effect.amplitude );

					break;

				case TextureEffectType.EnvironmentMap:
					break;
			}
		}

		/// <summary>
		///    Internal method for loading this texture stage as part of Material.Load.
		/// </summary>
		public void Load()
		{
			// Unload first
			Unload();

			// load all textures
			for ( var i = 0; i < this.numFrames; i++ )
			{
				if ( this.frames[ i ].Length > 0 )
				{
					try
					{
						// ensure the texture is loaded
						TextureManager.Instance.Load( this.frames[ i ], ResourceGroupManager.DefaultResourceGroupName, this.textureType,
						                              this.textureSrcMipmaps, 1.0f, this.isAlpha, this.desiredFormat /*, hwGamma */ );

						this.isBlank = false;
					}
					catch ( Exception ex )
					{
						LogManager.Instance.Write( "Error loading texture {0}.  Layer will be left blank.", this.frames[ i ] );
						LogManager.Instance.Write( ex.ToString() );
						this.isBlank = true;
					}
				}
			}

			// Init animated textures
			if ( this.animDuration != 0 )
			{
				CreateAnimationController();
			}

			// initialize texture effects
			foreach ( var effect in this.effectList )
			{
				CreateEffectController( effect );
			}
		}

		/// <summary>
		///    Internal method for unloading this object as part of Material.Unload.
		/// </summary>
		public void Unload()
		{
			// TODO: Implement TextureUnitState.Unload?
		}

		/// <summary>
		///    Notifies the parent that it needs recompilation.
		/// </summary>
		public void NotifyNeedsRecompile()
		{
			this.parent.NotifyNeedsRecompile();
		}

		/// <summary>
		/// Applies texture names to Texture Unit State with matching texture name aliases.
		/// If no matching aliases are found then the TUS state does not change.
		/// </summary>
		/// <remarks>
		/// Cubic, 1d, 2d, and 3d textures are determined from current state of the Texture Unit.
		/// Assumes animated frames are sequentially numbered in the name.
		/// If matching texture aliases are found then true is returned.
		/// </remarks>
		/// <param name="aliasList">is a map container of texture alias, texture name pairs</param>
		/// <param name="apply">set true to apply the texture aliases else just test to see if texture alias matches are found.</param>
		/// <returns>True if matching texture aliases were found in the Texture Unit State.</returns>
		public bool ApplyTextureAliases( Dictionary<string, string> aliasList, bool apply )
		{
			var testResult = false;
			// if TUS has an alias, see if it's in the alias container
			if ( !string.IsNullOrEmpty( this.textureNameAlias ) )
			{
				if ( aliasList.ContainsKey( this.textureNameAlias ) )
				{
					// match was found so change the texture name in frames
					testResult = true;

					if ( apply )
					{
						// currently assumes animated frames are sequentially numbered
						// cubic, 1d, 2d, and 3d textures are determined from current TUS state

						if ( this.isCubic )
						{
							SetCubicTextureName( aliasList[ this.textureNameAlias ], this.textureType == TextureType.CubeMap );
						}
						else
						{
							// if more than one frame, then assume animated frames
							if ( this.numFrames > 1 )
							{
								SetAnimatedTextureName( aliasList[ this.textureNameAlias ], this.numFrames, this.animDuration );
							}
							else
							{
								SetTextureName( aliasList[ this.textureNameAlias ], this.textureType, this.textureSrcMipmaps );
							}
						}
					}
				}
			}
			return testResult;
		}

		#endregion

		#region Object cloning

		/// <summary>
		///		Used to clone a texture layer.  Mainly used during a call to Clone on a Material.
		/// </summary>
		/// <returns></returns>
		public void CopyTo( TextureUnitState target )
		{
			var props = target.GetType().GetFields( BindingFlags.NonPublic | BindingFlags.Instance );

			// save parent from target, since it will be overwritten by the following loop
			var tmpParent = target.parent;

			for ( var i = 0; i < props.Length; i++ )
			{
				var prop = props[ i ];

				var srcVal = prop.GetValue( this );
				prop.SetValue( target, srcVal );
			}

			// restore correct parent
			target.parent = tmpParent;

			target.frames = new string[MaxAnimationFrames];

			// copy over animation frame texture names
			for ( var i = 0; i < MaxAnimationFrames; i++ )
			{
				target.frames[ i ] = this.frames[ i ];
			}

			// must clone these references
			target.colorBlendMode = this.colorBlendMode.Clone();
			target.alphaBlendMode = this.alphaBlendMode.Clone();

			target.effectList = new TextureEffectList();

			// copy effects
			foreach ( var effect in this.effectList )
			{
				target.effectList.Add( effect.Clone() );
			}

			// dirty the hash of the parent pass
			target.parent.DirtyHash();
		}

		/// <summary>
		///		Used to clone a texture layer.  Mainly used during a call to Clone on a Material or Pass.
		/// </summary>
		/// <returns></returns>
		public TextureUnitState Clone( Pass parent )
		{
			var newState = new TextureUnitState( parent );

			CopyTo( newState );

			newState.parent.DirtyHash();

			return newState;
		}

		#endregion Object cloning
	}

	#region LayerBlendModeEx class declaration

	/// <summary>
	///		Utility class for handling texture layer blending parameters.
	/// </summary>
	public class LayerBlendModeEx
	{
		public LayerBlendType blendType = LayerBlendType.Color;
		public LayerBlendOperationEx operation;
		public LayerBlendSource source1;
		public LayerBlendSource source2;
		public ColorEx colorArg1 = ColorEx.White;
		public ColorEx colorArg2 = ColorEx.White;
		public float alphaArg1 = 1.0f;
		public float alphaArg2 = 1.0f;
		public float blendFactor;

		/// <summary>
		///		Compares to blending modes for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==( LayerBlendModeEx left, LayerBlendModeEx right )
		{
			if ( (object)left == null && (object)right == null )
			{
				return true;
			}

			if ( (object)left == null || (object)right == null )
			{
				return false;
			}

			if ( left.colorArg1 != right.colorArg1 || left.colorArg2 != right.colorArg2 || left.blendFactor != right.blendFactor ||
			     left.source1 != right.source1 || left.source2 != right.source2 || left.operation != right.operation )
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		///		Compares to blending modes for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=( LayerBlendModeEx left, LayerBlendModeEx right )
		{
			if ( (object)left == null && (object)right == null )
			{
				return false;
			}

			if ( (object)left == null || (object)right == null )
			{
				return true;
			}

			if ( left.blendType != right.blendType )
			{
				return false;
			}

			if ( left.blendType == LayerBlendType.Color )
			{
				if ( left.colorArg1 != right.colorArg1 || left.colorArg2 != right.colorArg2 || left.blendFactor != right.blendFactor ||
				     left.source1 != right.source1 || left.source2 != right.source2 || left.operation != right.operation )
				{
					return true;
				}
			}
			else
			{
				if ( left.alphaArg1 != right.alphaArg1 || left.alphaArg2 != right.alphaArg2 || left.blendFactor != right.blendFactor ||
				     left.source1 != right.source1 || left.source2 != right.source2 || left.operation != right.operation )
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		///		Creates and returns a clone of this instance.
		/// </summary>
		/// <returns></returns>
		public LayerBlendModeEx Clone()
		{
			// copy the basic members
			var blendMode = (LayerBlendModeEx)MemberwiseClone();

			// clone the colors
			blendMode.colorArg1 = this.colorArg1.Clone();
			blendMode.colorArg2 = this.colorArg2.Clone();

			return blendMode;
		}

		#region Object overloads

		/// <summary>
		///    Overide to use custom equality check.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals( object obj )
		{
			var lbx = obj as LayerBlendModeEx;

			return ( lbx == this );
		}

		/// <summary>
		///    Override.
		/// </summary>
		/// <remarks>
		///    Overriden to quash warnings, not necessarily needed right now.
		/// </remarks>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return this.blendType.GetHashCode() ^ this.operation.GetHashCode() ^ this.source1.GetHashCode() ^
			       this.source2.GetHashCode() ^
			       this.colorArg1.GetHashCode() ^ this.colorArg2.GetHashCode() ^ this.alphaArg1.GetHashCode() ^
			       this.alphaArg2.GetHashCode() ^
			       this.blendFactor.GetHashCode();
		}

		public override string ToString()
		{
			return
				( new System.Text.StringBuilder() ).AppendFormat(
					"blendType : {0}; opertaion : {1}; source1 : {2}; source2 : {3}; colorArg1 : {4}; colorArg2 : {5}; alphaArg1 : {6}; alphaArg2 : {7}; blendType : {8};",
					this.blendType, this.operation, this.source1, this.source2, this.colorArg1, this.colorArg2, this.alphaArg1,
					this.alphaArg2, this.blendFactor ).ToString();
		}

		#endregion Object overloads
	}

	#endregion LayerBlendModeEx class declaration

	#region TextureEffect class declaration

	/// <summary>
	///		Class used to define parameters for a texture effect.
	/// </summary>
	public class TextureEffect
	{
		public TextureEffectType type;
		public System.Enum subtype;
		public Real arg1, arg2;
		public WaveformType waveType;
		public float baseVal;
		public float frequency;
		public float phase;
		public float amplitude;
		public Controller<Real> controller;
		public Frustum frustum;

		/// <summary>
		///		Returns a clone of this instance.
		/// </summary>
		/// <returns></returns>
		public TextureEffect Clone()
		{
			var clone = (TextureEffect)MemberwiseClone();

			return clone;
		}
	};

	#endregion TextureEffect class declaration
}