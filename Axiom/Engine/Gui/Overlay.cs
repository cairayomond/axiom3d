#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using Axiom.Core;

namespace Axiom.Gui {
    /// <summary>
    /// Summary description for Overlay.
    /// </summary>
    public class Overlay : Resource {

        #region Member variables

        protected int zOrder;

        #endregion Member variables

        #region Constructors
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public Overlay(String name) {
            this.name = name;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        ///    Z ordering of this overlay.
        /// </summary>
        public int ZOrder {
            get {
                return zOrder;
            }
            set {
                zOrder = value;
            }
        }

        #endregion Methods

        #region Implementation of Resource

        /// <summary>
        ///		
        /// </summary>
        public override void Load() {
		
        }

        /// <summary>
        ///		
        /// </summary>
        public override void Unload() {
		
        }

        /// <summary>
        ///		
        /// </summary>
        public override void Dispose() {
        }

        #endregion
    }
}
