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

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	/// <summary>
	/// Factory class for SimplePageContentCollection
	/// </summary>
	public class SimplePageContentCollectionFactory : DisposableObject, IPageContentCollectionFactory
	{
		[OgreVersion( 1, 7, 2 )] public static string FACTORY_NAME = "Simple";

		public string Name
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return FACTORY_NAME;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public PageContentCollection CreateInstance()
		{
			return new SimplePageContentCollection( this );
		}

		[OgreVersion( 1, 7, 2 )]
		public void DestroyInstance( ref PageContentCollection c )
		{
			c.SafeDispose();
		}
	};
}