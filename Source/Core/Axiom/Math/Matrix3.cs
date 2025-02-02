#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

The math library included in this project, in addition to being a derivative of
the works of Ogre, also include derivative work of the free portion of the 
Wild Magic mathematics source code that is distributed with the excellent
book Game Engine Design.
http://www.wild-magic.com/

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
using System.Runtime.InteropServices;
using System.Text;

#endregion Namespace Declarations

// NOTE.  The (x,y,z) coordinate system is assumed to be right-handed.
// Coordinate axis rotation matrices are of the form
//   RX =    1       0       0
//           0     cos(t) -sin(t)
//           0     sin(t)  cos(t)
// where t > 0 indicates a counterclockwise rotation in the yz-plane
//   RY =  cos(t)    0     sin(t)
//           0       1       0
//        -sin(t)    0     cos(t)
// where t > 0 indicates a counterclockwise rotation in the zx-plane
//   RZ =  cos(t) -sin(t)    0
//         sin(t)  cos(t)    0
//           0       0       1
// where t > 0 indicates a counterclockwise rotation in the xy-plane.

namespace Axiom.Math
{
	/// <summary>
	/// A 3x3 matrix which can represent rotations around axes.
	/// </summary>
	[StructLayout( LayoutKind.Sequential )]
	public struct Matrix3
	{
		#region Member variables and constants

		/// <summary>
		/// 
		/// </summary>
		public Real m00, m01, m02;

		public Real m10, m11, m12;
		public Real m20, m21, m22;

		private static readonly Matrix3 identityMatrix = new Matrix3( 1, 0, 0, 0, 1, 0, 0, 0, 1 );

		private static readonly Matrix3 zeroMatrix = new Matrix3( 0, 0, 0, 0, 0, 0, 0, 0, 0 );

		#endregion

		#region Constructors

		/// <summary>
		///		Creates a new Matrix3 with all the specified parameters.
		/// </summary>
		public Matrix3( Real m00, Real m01, Real m02, Real m10, Real m11, Real m12, Real m20, Real m21, Real m22 )
		{
			this.m00 = m00;
			this.m01 = m01;
			this.m02 = m02;
			this.m10 = m10;
			this.m11 = m11;
			this.m12 = m12;
			this.m20 = m20;
			this.m21 = m21;
			this.m22 = m22;
		}

		/// <summary>
		/// Create a new Matrix from 3 Vertex3 objects.
		/// </summary>
		/// <param name="xAxis"></param>
		/// <param name="yAxis"></param>
		/// <param name="zAxis"></param>
		public Matrix3( Vector3 xAxis, Vector3 yAxis, Vector3 zAxis )
		{
			this.m00 = xAxis.x;
			this.m01 = yAxis.x;
			this.m02 = zAxis.x;
			this.m10 = xAxis.y;
			this.m11 = yAxis.y;
			this.m12 = zAxis.y;
			this.m20 = xAxis.z;
			this.m21 = yAxis.z;
			this.m22 = zAxis.z;
		}

		#endregion

		#region Static properties

		/// <summary>
		/// Identity Matrix
		/// </summary>
		public static Matrix3 Identity
		{
			get
			{
				return identityMatrix;
			}
		}

		/// <summary>
		/// Zero matrix.
		/// </summary>
		public static Matrix3 Zero
		{
			get
			{
				return zeroMatrix;
			}
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Swap the rows of the matrix with the columns.
		/// </summary>
		/// <returns>A transposed Matrix.</returns>
		public Matrix3 Transpose()
		{
			return new Matrix3( this.m00, this.m10, this.m20, this.m01, this.m11, this.m21, this.m02, this.m12, this.m22 );
		}

		/// <summary>
		///		Gets a matrix column by index.
		/// </summary>
		/// <param name="col"></param>
		/// <returns>A Vector3 representing one of the Matrix columns.</returns>
		public Vector3 GetColumn( int col )
		{
			Debug.Assert( col >= 0 && col < 3, "Attempt to retreive a column of a Matrix3 greater than 2." );

#if AXIOM_SAFE_ONLY
            switch(col)
            {
                case 0: return new Vector3(m00, m01, m02);
                case 1: return new Vector3(m10, m11, m12);
                case 2: return new Vector3(m20, m21, m22);
            }
		    throw new IndexOutOfRangeException("Attempt to retreive a column of a Matrix3 greater than 2.");
#else
			unsafe
			{
				fixed ( Real* pM = &this.m00 )
				{
					return new Vector3( *( pM + col ), //m[0,col], 
					                    *( pM + 3 + col ), //m[1,col], 
					                    *( pM + 6 + col ) ); //m[2,col]);
				}
			}
#endif
		}

		/// <summary>
		///		Sets one of the columns of the Matrix with a Vector3.
		/// </summary>
		/// <param name="col"></param>
		/// <param name="vector"></param>
		/// <returns></returns>
		public void SetColumn( int col, Vector3 vector )
		{
			Debug.Assert( col >= 0 && col < 3, "Attempt to set a column of a Matrix3 greater than 2." );

			this[ 0, col ] = vector.x;
			this[ 1, col ] = vector.y;
			this[ 2, col ] = vector.z;
		}

		/// <summary>
		///		Creates a Matrix3 from 3 axes.
		/// </summary>
		/// <param name="xAxis"></param>
		/// <param name="yAxis"></param>
		/// <param name="zAxis"></param>
		public void FromAxes( Vector3 xAxis, Vector3 yAxis, Vector3 zAxis )
		{
			SetColumn( 0, xAxis );
			SetColumn( 1, yAxis );
			SetColumn( 2, zAxis );
		}

		/// <summary>
		///    Constructs this Matrix from 3 euler angles, in degrees.
		/// </summary>
		/// <param name="yaw"></param>
		/// <param name="pitch"></param>
		/// <param name="roll"></param>
		public void FromEulerAnglesXYZ( Real yaw, Real pitch, Real roll )
		{
			var cos = Utility.Cos( yaw );
			var sin = Utility.Sin( yaw );
			var xMat = new Matrix3( 1, 0, 0, 0, cos, -sin, 0, sin, cos );

			cos = Utility.Cos( pitch );
			sin = Utility.Sin( pitch );
			var yMat = new Matrix3( cos, 0, sin, 0, 1, 0, -sin, 0, cos );

			cos = Utility.Cos( roll );
			sin = Utility.Sin( roll );
			var zMat = new Matrix3( cos, -sin, 0, sin, cos, 0, 0, 0, 1 );

			this = xMat*( yMat*zMat );
		}

		public Vector3 ToEulerAnglesXYZ()
		{
			Real yAngle;
			Real rAngle;
			Real pAngle;

			pAngle = Utility.ASin( this.m01 );
			if ( pAngle < Utility.PI/2 )
			{
				if ( pAngle > -Utility.PI/2 )
				{
					yAngle = Utility.ATan2( this.m21, this.m11 );
					rAngle = Utility.ATan2( this.m02, this.m00 );
				}
				else
				{
					// WARNING. Not a unique solution.
					var fRmY = (Real)Utility.ATan2( -this.m20, this.m22 );
					rAngle = 0.0f; // any angle works
					yAngle = rAngle - fRmY;
				}
			}
			else
			{
				// WARNING. Not a unique solution.
				var fRpY = Utility.ATan2( -this.m20, this.m22 );
				rAngle = 0.0f; // any angle works
				yAngle = fRpY - rAngle;
			}

			return new Vector3( yAngle, rAngle, pAngle );
		}

		#endregion

		#region Operator overloads + CLS complient method equivalents

		/// <summary>
		/// Multiply (concatenate) two Matrix3 instances together.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Matrix3 Multiply( Matrix3 left, Matrix3 right )
		{
			return left*right;
		}

		/// <summary>
		/// Multiply (concatenate) two Matrix3 instances together.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Matrix3 operator *( Matrix3 left, Matrix3 right )
		{
			var result = new Matrix3();

			result.m00 = left.m00*right.m00 + left.m01*right.m10 + left.m02*right.m20;
			result.m01 = left.m00*right.m01 + left.m01*right.m11 + left.m02*right.m21;
			result.m02 = left.m00*right.m02 + left.m01*right.m12 + left.m02*right.m22;

			result.m10 = left.m10*right.m00 + left.m11*right.m10 + left.m12*right.m20;
			result.m11 = left.m10*right.m01 + left.m11*right.m11 + left.m12*right.m21;
			result.m12 = left.m10*right.m02 + left.m11*right.m12 + left.m12*right.m22;

			result.m20 = left.m20*right.m00 + left.m21*right.m10 + left.m22*right.m20;
			result.m21 = left.m20*right.m01 + left.m21*right.m11 + left.m22*right.m21;
			result.m22 = left.m20*right.m02 + left.m21*right.m12 + left.m22*right.m22;

			return result;
		}

		/// <summary>
		///		vector * matrix [1x3 * 3x3 = 1x3]
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static Vector3 Multiply( Vector3 vector, Matrix3 matrix )
		{
			return vector*matrix;
		}

		/// <summary>
		///		vector * matrix [1x3 * 3x3 = 1x3]
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static Vector3 operator *( Vector3 vector, Matrix3 matrix )
		{
			var product = new Vector3();

			product.x = matrix.m00*vector.x + matrix.m01*vector.y + matrix.m02*vector.z;
			product.y = matrix.m10*vector.x + matrix.m11*vector.y + matrix.m12*vector.z;
			product.z = matrix.m20*vector.x + matrix.m21*vector.y + matrix.m22*vector.z;

			return product;
		}

		/// <summary>
		///		matrix * vector [3x3 * 3x1 = 3x1]
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static Vector3 Multiply( Matrix3 matrix, Vector3 vector )
		{
			return matrix*vector;
		}

		/// <summary>
		///		matrix * vector [3x3 * 3x1 = 3x1]
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static Vector3 operator *( Matrix3 matrix, Vector3 vector )
		{
			var product = new Vector3();

			product.x = matrix.m00*vector.x + matrix.m01*vector.y + matrix.m02*vector.z;
			product.y = matrix.m10*vector.x + matrix.m11*vector.y + matrix.m12*vector.z;
			product.z = matrix.m20*vector.x + matrix.m21*vector.y + matrix.m22*vector.z;

			return product;
		}

		/// <summary>
		/// Multiplies all the items in the Matrix3 by a scalar value.
		/// </summary>
		/// <param name="matrix"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Matrix3 Multiply( Matrix3 matrix, Real scalar )
		{
			return matrix*scalar;
		}

		/// <summary>
		/// Multiplies all the items in the Matrix3 by a scalar value.
		/// </summary>
		/// <param name="matrix"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Matrix3 operator *( Matrix3 matrix, Real scalar )
		{
			var result = new Matrix3();

			result.m00 = matrix.m00*scalar;
			result.m01 = matrix.m01*scalar;
			result.m02 = matrix.m02*scalar;
			result.m10 = matrix.m10*scalar;
			result.m11 = matrix.m11*scalar;
			result.m12 = matrix.m12*scalar;
			result.m20 = matrix.m20*scalar;
			result.m21 = matrix.m21*scalar;
			result.m22 = matrix.m22*scalar;

			return result;
		}

		/// <summary>
		/// Multiplies all the items in the Matrix3 by a scalar value.
		/// </summary>
		/// <param name="matrix"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Matrix3 Multiply( Real scalar, Matrix3 matrix )
		{
			return scalar*matrix;
		}

		/// <summary>
		/// Multiplies all the items in the Matrix3 by a scalar value.
		/// </summary>
		/// <param name="matrix"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Matrix3 operator *( Real scalar, Matrix3 matrix )
		{
			var result = new Matrix3();

			result.m00 = matrix.m00*scalar;
			result.m01 = matrix.m01*scalar;
			result.m02 = matrix.m02*scalar;
			result.m10 = matrix.m10*scalar;
			result.m11 = matrix.m11*scalar;
			result.m12 = matrix.m12*scalar;
			result.m20 = matrix.m20*scalar;
			result.m21 = matrix.m21*scalar;
			result.m22 = matrix.m22*scalar;

			return result;
		}

		/// <summary>
		///		Used to add two matrices together.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Matrix3 Add( Matrix3 left, Matrix3 right )
		{
			return left + right;
		}

		/// <summary>
		///		Used to add two matrices together.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Matrix3 operator +( Matrix3 left, Matrix3 right )
		{
			var result = new Matrix3();

			for ( var row = 0; row < 3; row++ )
			{
				for ( var col = 0; col < 3; col++ )
				{
					result[ row, col ] = left[ row, col ] + right[ row, col ];
				}
			}

			return result;
		}

		/// <summary>
		///		Used to subtract two matrices.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Matrix3 Subtract( Matrix3 left, Matrix3 right )
		{
			return left - right;
		}

		/// <summary>
		///		Used to subtract two matrices.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Matrix3 operator -( Matrix3 left, Matrix3 right )
		{
			var result = new Matrix3();

			for ( var row = 0; row < 3; row++ )
			{
				for ( var col = 0; col < 3; col++ )
				{
					result[ row, col ] = left[ row, col ] - right[ row, col ];
				}
			}

			return result;
		}

		/// <summary>
		/// Negates all the items in the Matrix.
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static Matrix3 Negate( Matrix3 matrix )
		{
			return -matrix;
		}

		/// <summary>
		/// Negates all the items in the Matrix.
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static Matrix3 operator -( Matrix3 matrix )
		{
			var result = new Matrix3();

			result.m00 = -matrix.m00;
			result.m01 = -matrix.m01;
			result.m02 = -matrix.m02;
			result.m10 = -matrix.m10;
			result.m11 = -matrix.m11;
			result.m12 = -matrix.m12;
			result.m20 = -matrix.m20;
			result.m21 = -matrix.m21;
			result.m22 = -matrix.m22;

			return result;
		}

		/// <summary>
		/// 	Test two matrices for (value) equality
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==( Matrix3 left, Matrix3 right )
		{
			if ( left.m00 == right.m00 && left.m01 == right.m01 && left.m02 == right.m02 && left.m10 == right.m10 &&
			     left.m11 == right.m11 && left.m12 == right.m12 && left.m20 == right.m20 && left.m21 == right.m21 &&
			     left.m22 == right.m22 )
			{
				return true;
			}

			return false;
		}

		public static bool operator !=( Matrix3 left, Matrix3 right )
		{
			return !( left == right );
		}

		/// <summary>
		/// Indexer for accessing the matrix like a 2d array (i.e. matrix[2,3]).
		/// </summary>
		public Real this[ int row, int col ]
		{
			get
			{
				//Debug.Assert((row >= 0 && row < 3) && (col >= 0 && col < 3), "Attempt to access Matrix3 indexer out of bounds.");

#if AXIOM_SAFE_ONLY
                switch (row)
                {
                    case 0:
                        switch (col)
                        {
                            case 0: return m00;
                            case 1: return m01;
                            case 2: return m02;
                        }
                        break;
                    case 1:
                        switch (col)
                        {
                            case 0: return m10;
                            case 1: return m11;
                            case 2: return m12;
                        }
                        break;
                    case 2:
                        switch (col)
                        {
                            case 0: return m20;
                            case 1: return m21;
                            case 2: return m22;
                        }
                        break;
                }
			    throw new IndexOutOfRangeException("Attempt to access Matrix3 indexer out of bounds.");
#else
				unsafe
				{
					fixed ( Real* pM = &this.m00 )
					{
						return *( pM + ( ( 3*row ) + col ) );
					}
				}
#endif
			}
			set
			{
				//Debug.Assert((row >= 0 && row < 3) && (col >= 0 && col < 3), "Attempt to access Matrix3 indexer out of bounds.");

#if AXIOM_SAFE_ONLY
                switch (row)
                {
                    case 0:
                        switch (col)
                        {
                            case 0: m00 = value; break;
                            case 1: m01 = value; break;
                            case 2: m02 = value; break;
                        }
                        break;
                    case 1:
                        switch (col)
                        {
                            case 0: m10 = value; break;
                            case 1: m11 = value; break;
                            case 2: m12 = value; break;
                        }
                        break;
                    case 2:
                        switch (col)
                        {
                            case 0: m20 = value; break;
                            case 1: m21 = value; break;
                            case 2: m22 = value; break;
                        }
                        break;
                }
#else
				unsafe
				{
					fixed ( Real* pM = &this.m00 )
					{
						*( pM + ( ( 3*row ) + col ) ) = value;
					}
				}
#endif
			}
		}

		/// <summary>
		///		Allows the Matrix to be accessed linearly (m[0] -> m[8]).  
		/// </summary>
		public Real this[ int index ]
		{
			get
			{
				//Debug.Assert(index >= 0 && index <= 8, "Attempt to access Matrix3 linear indexer out of bounds.");

#if AXIOM_SAFE_ONLY
                switch (index)
                {
                    case 0: return m00;
                    case 1: return m01;
                    case 2: return m02;
                    case 3: return m10;
                    case 4: return m11;
                    case 5: return m12;
                    case 6: return m20;
                    case 7: return m21;
                    case 8: return m22;                    
                }
                throw new IndexOutOfRangeException("Attempt to access Matrix3 indexer out of bounds.");
#else
				unsafe
				{
					fixed ( Real* pMatrix = &this.m00 )
					{
						return *( pMatrix + index );
					}
				}
#endif
			}
			set
			{
				//Debug.Assert(index >= 0 && index <= 8, "Attempt to access Matrix3 linear indexer out of bounds.");

#if AXIOM_SAFE_ONLY
                switch (index)
                {
                    case 0: m00 = value; break;
                    case 1: m01 = value; break;
                    case 2: m02 = value; break;
                    case 3: m10 = value; break;
                    case 4: m11 = value; break;
                    case 5: m12 = value; break;
                    case 6: m20 = value; break;
                    case 7: m21 = value; break;
                    case 8: m22 = value; break;
                }
#else
				unsafe
				{
					fixed ( Real* pMatrix = &this.m00 )
					{
						*( pMatrix + index ) = value;
					}
				}
#endif
			}
		}

		#endregion

		#region Properties

		public Real Determinant
		{
			get
			{
				var cofactor00 = this.m11*this.m22 - this.m12*this.m21;
				var cofactor10 = this.m12*this.m20 - this.m10*this.m22;
				var cofactor20 = this.m10*this.m21 - this.m11*this.m20;

				var result = this.m00*cofactor00 + this.m01*cofactor10 + this.m02*cofactor20;

				return result;
			}
		}

		#endregion Properties

		#region Object overloads

		/// <summary>
		///		Overrides the Object.ToString() method to provide a text representation of 
		///		a Matrix4.
		/// </summary>
		/// <returns>A string representation of a vector3.</returns>
		public override string ToString()
		{
			var builder = new StringBuilder();

			builder.AppendFormat( " | {0} {1} {2} |\n", this.m00, this.m01, this.m02 );
			builder.AppendFormat( " | {0} {1} {2} |\n", this.m10, this.m11, this.m12 );
			builder.AppendFormat( " | {0} {1} {2} |", this.m20, this.m21, this.m22 );

			return builder.ToString();
		}

		/// <summary>
		///		Provides a unique hash code based on the member variables of this
		///		class.  This should be done because the equality operators (==, !=)
		///		have been overriden by this class.
		///		<p/>
		///		The standard implementation is a simple XOR operation between all local
		///		member variables.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
#if AXIOM_SAFE_ONLY
		    return (int) (m00) ^ (int) (m01) ^ (int) (m02) ^ 
		           (int) (m10) ^ (int) (m11) ^ (int) (m12) ^ 
		           (int) (m20) ^ (int) (m21) ^ (int) (m22);
#else
			var hashCode = 0;
			unsafe
			{
				fixed ( Real* pM = &this.m00 )
				{
					for ( var i = 0; i < 9; i++ )
					{
						hashCode ^= (int)( *( pM + i ) );
					}
				}
			}
			return hashCode;
#endif
		}

		/// <summary>
		///		Compares this Matrix to another object.  This should be done because the 
		///		equality operators (==, !=) have been overriden by this class.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals( object obj )
		{
			return obj is Matrix3 && this == (Matrix3)obj;
		}

		#endregion
	}
}