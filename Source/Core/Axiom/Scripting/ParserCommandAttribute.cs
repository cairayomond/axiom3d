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
using System.Reflection;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.Scripting
{
	/// <summary>
	///		Custom attribute to mark methods as handling the parsing for a material script attribute.
	/// </summary>
	[AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
	public sealed class ParserCommandAttribute : Attribute
	{
		private readonly string attributeName;
		private readonly string parserType;

		public ParserCommandAttribute( string name, string parserType )
		{
			this.attributeName = name;
			this.parserType = parserType;
		}

		public string Name
		{
			get
			{
				return this.attributeName;
			}
		}

		public string ParserType
		{
			get
			{
				return this.parserType;
			}
		}
	}
}