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
using System.Diagnostics;
using Axiom.Core;
using Axiom.Fonts;
using Axiom.Scripting;
using Axiom.Graphics;
using Font = Axiom.Fonts.Font;
using Axiom.Math;

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="OgreTextAreaOverlayElement.h"   revision="1.7.2.1" lastUpdated="10/21/2005" lastUpdatedBy="DanielH" />
//     <file name="OgreTextAreaOverlayElement.cpp" revision="1.9" lastUpdated="10/21/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

#endregion

namespace Axiom.Overlays.Elements
{
	/// <summary>
	/// 	Label type control that can be used to display text using a specified font.
	/// </summary>
	public class TextArea : OverlayElement
	{
		#region Member variables

		protected HorizontalAlignment textAlign;
		protected bool isTransparent;
		protected Font font;
		protected float charHeight;
		protected float pixelCharHeight;
		protected float spaceWidth;
		protected int pixelSpaceWidth;
		protected int allocSize;
		protected float viewportAspectCoef;

		/// Colors to use for the vertices
		protected ColorEx colorBottom;

		protected ColorEx colorTop;
		protected bool haveColorsChanged;

		private const int DEFAULT_INITIAL_CHARS = 12;
		private const int POSITION_TEXCOORD_BINDING = 0;
		private const int COLOR_BINDING = 1;

		#endregion

		#region Constructors

		/// <summary>
		///    Basic constructor, internal since it should only be created by factories.
		/// </summary>
		/// <param name="name"></param>
		internal TextArea( string name )
			: base( name )
		{
			//isTransparent = false; //[FXCop Optimization : Do not initialize unnecessarily], Defaults to false, left here for clarity
			this.textAlign = HorizontalAlignment.Center;


			this.colorTop = ColorEx.White;
			this.colorBottom = ColorEx.White;
			this.haveColorsChanged = true;

			this.charHeight = 0.02f;
			this.pixelCharHeight = 12;
			this.viewportAspectCoef = 1f;
		}

		#endregion

		#region Methods

		/// <summary>
		/// </summary>
		protected void CheckMemoryAllocation( int numChars )
		{
			if ( this.allocSize < numChars )
			{
				// Create and bind new buffers
				// Note that old buffers will be deleted automatically through reference counting

				// 6 verts per char since we're doing tri lists without indexes
				// Allocate space for positions & texture coords
				var decl = renderOperation.vertexData.vertexDeclaration;
				var binding = renderOperation.vertexData.vertexBufferBinding;

				renderOperation.vertexData.vertexCount = numChars*6;

				// Create dynamic since text tends to change alot
				// positions & texcoords
				var buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( POSITION_TEXCOORD_BINDING ),
				                                                                renderOperation.vertexData.vertexCount,
				                                                                BufferUsage.DynamicWriteOnly );

				// bind the pos/tex buffer
				binding.SetBinding( POSITION_TEXCOORD_BINDING, buffer );

				// colors
				buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( COLOR_BINDING ),
				                                                            renderOperation.vertexData.vertexCount,
				                                                            BufferUsage.DynamicWriteOnly );

				// bind the color buffer
				binding.SetBinding( COLOR_BINDING, buffer );

				this.allocSize = numChars;
				// force color buffer regeneration
				this.haveColorsChanged = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			if ( !isInitialized )
			{
				// Set up the render operation
				// Combine positions and texture coords since they tend to change together
				// since character sizes are different
				renderOperation.vertexData = new VertexData();
				var decl = renderOperation.vertexData.vertexDeclaration;

				var offset = 0;

				// positions
				decl.AddElement( POSITION_TEXCOORD_BINDING, offset, VertexElementType.Float3, VertexElementSemantic.Position );
				offset += VertexElement.GetTypeSize( VertexElementType.Float3 );

				// texcoords
				decl.AddElement( POSITION_TEXCOORD_BINDING, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
				offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
				// colors, stored in seperate buffer since they change less often
				decl.AddElement( COLOR_BINDING, 0, VertexElementType.Color, VertexElementSemantic.Diffuse );

				renderOperation.operationType = OperationType.TriangleList;
				renderOperation.useIndices = false;
				renderOperation.vertexData.vertexStart = 0;

				// buffers are created in CheckMemoryAllocation
				CheckMemoryAllocation( DEFAULT_INITIAL_CHARS );

				isInitialized = true;
			}
		}

		/// <summary>
		///    Override to update char sizing based on current viewport settings.
		/// </summary>
		public override void Update()
		{
			float vpWidth = OverlayManager.Instance.ViewportWidth;
			float vpHeight = OverlayManager.Instance.ViewportHeight;
			this.viewportAspectCoef = vpHeight/vpWidth;

			if ( metricsMode != MetricsMode.Relative &&
			     ( OverlayManager.Instance.HasViewportChanged || isGeomPositionsOutOfDate ) )
			{
				this.charHeight = (float)this.pixelCharHeight/vpHeight;
				this.spaceWidth = (float)this.pixelSpaceWidth/vpHeight;

				isGeomPositionsOutOfDate = true;
			}

			base.Update();

			if ( this.haveColorsChanged && isInitialized )
			{
				UpdateColors();
				this.haveColorsChanged = false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void UpdateColors()
		{
			// convert to API specific color values
			var topColor = Root.Instance.ConvertColor( this.colorTop );
			var bottomColor = Root.Instance.ConvertColor( this.colorBottom );

			// get the seperate color buffer
			var buffer = renderOperation.vertexData.vertexBufferBinding.GetBuffer( COLOR_BINDING );

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var data = buffer.Lock( BufferLocking.Discard );
				var colPtr = data.ToIntPointer();
				var index = 0;

				for ( var i = 0; i < this.allocSize; i++ )
				{
					// first tri (top, bottom, top);
					colPtr[ index++ ] = topColor;
					colPtr[ index++ ] = bottomColor;
					colPtr[ index++ ] = topColor;

					// second tri (top, bottom, bottom);
					colPtr[ index++ ] = topColor;
					colPtr[ index++ ] = bottomColor;
					colPtr[ index++ ] = bottomColor;
				}
			}
			// unlock this bad boy
			buffer.Unlock();
		}

		/// <summary>
		/// 
		/// </summary>
		protected void UpdateGeometry()
		{
			if ( this.font == null || text == null || !isGeomPositionsOutOfDate )
			{
				// must not be initialized yet, probably due to order of creation in a template
				return;
			}

			var charLength = text.Length;
			// make sure the buffers are big enough
			CheckMemoryAllocation( charLength );

			renderOperation.vertexData.vertexCount = charLength*6;

			// get pos/tex buffer
			var buffer = renderOperation.vertexData.vertexBufferBinding.GetBuffer( POSITION_TEXCOORD_BINDING );
			var data = buffer.Lock( BufferLocking.Discard );
			var largestWidth = 0.0f;
			var left = DerivedLeft*2.0f - 1.0f;
			var top = -( ( DerivedTop*2.0f ) - 1.0f );

			// derive space width from the size of a capital A
			if ( this.spaceWidth == 0 )
			{
				this.spaceWidth = this.font.GetGlyphAspectRatio( 'A' )*this.charHeight*2.0f*this.viewportAspectCoef;
			}


			var newLine = true;
			var index = 0;

			// go through each character and process
			for ( var i = 0; i < charLength; i++ )
			{
				var c = text[ i ];

				if ( newLine )
				{
					var length = 0.0f;

					// precalc the length of this line
					for ( var j = i; j < charLength && text[ j ] != '\n'; j++ )
					{
						if ( text[ j ] == ' ' )
						{
							length += this.spaceWidth;
						}
						else
						{
							length += this.font.GetGlyphAspectRatio( text[ j ] )*this.charHeight*2f*this.viewportAspectCoef;
						}
					} // for j

					if ( horzAlign == HorizontalAlignment.Right )
					{
						left -= length;
					}
					else if ( horzAlign == HorizontalAlignment.Center )
					{
						left -= length*0.5f;
					}

					newLine = false;
				} // if newLine

				if ( c == '\n' )
				{
					left = DerivedLeft*2.0f - 1.0f;
					top -= this.charHeight*2.0f;
					newLine = true;
					// reduce tri count
					renderOperation.vertexData.vertexCount -= 6;
					continue;
				}

				if ( c == ' ' )
				{
					// leave a gap, no tris required
					left += this.spaceWidth;
					// reduce tri count
					renderOperation.vertexData.vertexCount -= 6;
					continue;
				}

				var horizHeight = this.font.GetGlyphAspectRatio( c )*this.viewportAspectCoef;
				Real u1, u2, v1, v2;

				// get the texcoords for the specified character
				this.font.GetGlyphTexCoords( c, out u1, out v1, out u2, out v2 );

#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					// each vert is (x, y, z, u, v)
					// first tri
					// upper left
					var vertPtr = data.ToFloatPointer();
					vertPtr[ index++ ] = left;
					vertPtr[ index++ ] = top;
					vertPtr[ index++ ] = -1.0f;
					vertPtr[ index++ ] = u1;
					vertPtr[ index++ ] = v1;

					top -= this.charHeight*2.0f;

					// bottom left
					vertPtr[ index++ ] = left;
					vertPtr[ index++ ] = top;
					vertPtr[ index++ ] = -1.0f;
					vertPtr[ index++ ] = u1;
					vertPtr[ index++ ] = v2;

					top += this.charHeight*2.0f;
					left += horizHeight*this.charHeight*2.0f;

					// top right
					vertPtr[ index++ ] = left;
					vertPtr[ index++ ] = top;
					vertPtr[ index++ ] = -1.0f;
					vertPtr[ index++ ] = u2;
					vertPtr[ index++ ] = v1;

					// second tri

					// top right (again)
					vertPtr[ index++ ] = left;
					vertPtr[ index++ ] = top;
					vertPtr[ index++ ] = -1.0f;
					vertPtr[ index++ ] = u2;
					vertPtr[ index++ ] = v1;

					top -= this.charHeight*2.0f;
					left -= horizHeight*this.charHeight*2.0f;

					// bottom left (again)
					vertPtr[ index++ ] = left;
					vertPtr[ index++ ] = top;
					vertPtr[ index++ ] = -1.0f;
					vertPtr[ index++ ] = u1;
					vertPtr[ index++ ] = v2;

					left += horizHeight*this.charHeight*2.0f;

					// bottom right
					vertPtr[ index++ ] = left;
					vertPtr[ index++ ] = top;
					vertPtr[ index++ ] = -1.0f;
					vertPtr[ index++ ] = u2;
					vertPtr[ index++ ] = v2;
				}

				// go back up with top
				top += this.charHeight*2.0f;

				var currentWidth = ( left + 1 )/2 - DerivedLeft;

				if ( currentWidth > largestWidth )
				{
					largestWidth = currentWidth;
				}
			} // for i

			// unlock vertex buffer
			buffer.Unlock();

			if ( metricsMode == MetricsMode.Pixels )
			{
				// Derive parametric version of dimensions
				float vpWidth = OverlayManager.Instance.ViewportWidth;

				largestWidth *= vpWidth;
			}

			// record the width as the longest width calculated for any of the lines
			if ( Width < largestWidth )
			{
				Width = largestWidth;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void UpdatePositionGeometry()
		{
			UpdateGeometry();
		}

		protected override void UpdateTextureGeometry()
		{
			// Nothing to do, we combine positions and textures
		}

		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public float CharHeight
		{
			get
			{
				if ( metricsMode == MetricsMode.Pixels )
				{
					return (float)this.pixelCharHeight;
				}
				else
				{
					return this.charHeight;
				}
			}
			set
			{
				if ( metricsMode != MetricsMode.Relative )
				{
					this.pixelCharHeight = value;
				}
				else
				{
					this.charHeight = value;
				}
				isGeomPositionsOutOfDate = true;
			}
		}

		/// <summary>
		///    Gets/Sets the color value of the text when it is all the same color.
		/// </summary>
		public override ColorEx Color
		{
			get
			{
				// doesnt matter if they are both the same
				return this.colorTop;
			}
			set
			{
				this.colorTop = value;
				this.colorBottom = value;
				this.haveColorsChanged = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public ColorEx ColorTop
		{
			get
			{
				return this.colorTop;
			}
			set
			{
				this.colorTop = value;
				this.haveColorsChanged = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public ColorEx ColorBottom
		{
			get
			{
				return this.colorBottom;
			}
			set
			{
				this.colorBottom = value;
				this.haveColorsChanged = true;
			}
		}

		/// <summary>
		///    Gets/Sets the name of the font currently being used when rendering the text.
		/// </summary>
		public string FontName
		{
			get
			{
				return this.font != null ? this.font.Name : null;
			}
			set
			{
				this.font = (Font)FontManager.Instance[ value ];
				if ( this.font == null )
				{
					throw new Exception( "Could not find font " + value );
				}
				this.font.Load();

				// note: font materials are created with lighting and depthcheck disabled by default
				material = this.font.Material;

				// TODO See if this can be eliminated

				material.DepthCheck = false;
				material.Lighting = false;

				isGeomPositionsOutOfDate = true;
				isGeomUVsOutOfDate = true;
			}
		}

		/// <summary>
		///    Override to update geometry when new material is assigned.
		/// </summary>
		public override string MaterialName
		{
			get
			{
				return base.MaterialName;
			}
			set
			{
				base.MaterialName = value;
			}
		}

		/// <summary>
		///    Override to handle character spacing
		/// </summary>
		public override MetricsMode MetricsMode
		{
			get
			{
				return base.MetricsMode;
			}
			set
			{
				float vpWidth = OverlayManager.Instance.ViewportWidth;
				float vpHeight = OverlayManager.Instance.ViewportHeight;
				this.viewportAspectCoef = vpHeight/vpWidth;
				base.MetricsMode = value;

				// configure pixel variables based on current viewport
				switch ( metricsMode )
				{
					case MetricsMode.Pixels:
						// set pixel variables multiplied by the viewport multipliers
						this.pixelCharHeight = (int)( this.charHeight*vpHeight );
						this.pixelSpaceWidth = (int)( this.spaceWidth*vpHeight );
						break;
					case MetricsMode.Relative:
						// set pixel variables multiplied by the height constant
						this.pixelCharHeight = (int)( this.charHeight*10000.0 );
						this.pixelSpaceWidth = (int)( this.spaceWidth*10000.0 );
						break;
				}
			}
		}

		/// <summary>
		///    
		/// </summary>
		public float SpaceWidth
		{
			get
			{
				if ( metricsMode == MetricsMode.Pixels )
				{
					return (float)this.pixelSpaceWidth;
				}
				else
				{
					return this.spaceWidth;
				}
			}
			set
			{
				if ( metricsMode != MetricsMode.Relative )
				{
					this.pixelSpaceWidth = (int)value;
				}
				else
				{
					this.spaceWidth = value;
				}

				isGeomPositionsOutOfDate = true;
			}
		}

		/// <summary>
		///    Alignment of text specifically.
		/// </summary>
		public HorizontalAlignment TextAlign
		{
			get
			{
				return this.textAlign;
			}
			set
			{
				this.textAlign = value;
				isGeomPositionsOutOfDate = true;
			}
		}

		/// <summary>
		///    Override to update string geometry.
		/// </summary>
		public override string Text
		{
			get
			{
				return base.Text;
			}
			set
			{
				base.Text = value;
				isGeomPositionsOutOfDate = true;
				isGeomUVsOutOfDate = true;
			}
		}

		#endregion

		#region ScriptableObject Interface Command Classes

		[ScriptableProperty( "char_height", "", typeof ( TextArea ) )]
		public class CharacterHeightAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					return element.CharHeight.ToString();
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					element.CharHeight = StringConverter.ParseFloat( val );
				}
			}

			#endregion
		}

		[ScriptableProperty( "space_width", "", typeof ( TextArea ) )]
		public class SpaceWidthAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					return element.SpaceWidth.ToString();
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					element.SpaceWidth = StringConverter.ParseFloat( val );
				}
			}

			#endregion
		}

		[ScriptableProperty( "font_name", "", typeof ( TextArea ) )]
		public class FontNameAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					return element.FontName;
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					element.FontName = val;
				}
			}

			#endregion
		}

		[ScriptableProperty( "alignment", "The horizontal alignment, 'left', 'right' or 'center'.", typeof ( TextArea ) )]
		public class TextAlignmentAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					return ScriptEnumAttribute.GetScriptAttribute( (int)element.TextAlign, typeof ( HorizontalAlignment ) );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					element.TextAlign = (HorizontalAlignment)ScriptEnumAttribute.Lookup( val, typeof ( HorizontalAlignment ) );
				}
			}

			#endregion
		}

		[ScriptableProperty( "color", "", typeof ( TextArea ) )]
		[ScriptableProperty( "colour", "", typeof ( TextArea ) )]
		public class ColorAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					return StringConverter.ToString( element.Color );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					element.Color = StringConverter.ParseColor( val );
				}
			}

			#endregion
		}

		[ScriptableProperty( "color_top", "", typeof ( TextArea ) )]
		[ScriptableProperty( "colour_top", "", typeof ( TextArea ) )]
		public class TopColorAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					return StringConverter.ToString( element.ColorTop );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					element.ColorTop = StringConverter.ParseColor( val );
				}
			}

			#endregion
		}

		[ScriptableProperty( "color_bottom", "", typeof ( TextArea ) )]
		[ScriptableProperty( "colour_bottom", "", typeof ( TextArea ) )]
		public class BottomColorAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					return StringConverter.ToString( element.ColorBottom );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as TextArea;
				if ( element != null )
				{
					element.ColorBottom = StringConverter.ParseColor( val );
				}
			}

			#endregion
		}

		#endregion ScriptableObject Interface Command Classes
	}
}