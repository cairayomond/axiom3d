#region LGPL License

// Axiom Graphics Engine Library
// Copyright (C) 2003-2010 Axiom Project Team
// 
// The overall design, and a majority of the core engine and rendering code 
// contained within this library is a derivative of the open source Object Oriented 
// Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
// Many thanks to the OGRE team for maintaining such a high quality project.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

#endregion

#region Namespace Declarations

using Axiom.Core;
using Axiom.SceneManagers.PortalConnected;

using MbUnit.Framework;

#endregion

namespace Axiom.UnitTests.SceneManagers.PortalConnected
{
    /// <summary>
    /// Regression tests for the <see cref="PCZSceneNode"/> class.
    /// </summary>
    [ TestFixture ]
    internal class PczSceneNodeTests
    {
        /// <summary>
        /// Verifies that the destruction of a scene node via the interface of its parent does in fact also
        /// remove that child node from the <see cref="SceneManager"/> scene graph.
        /// </summary>
        [ Test ]
        public void TestChildSceneNodeDestruction()
        {
            SceneManager sceneManager = new PCZSceneManager( "Manager under test" );
            SceneNode node = sceneManager.CreateSceneNode( "testNode" );
            SceneNode childNode = node.CreateChildSceneNode( "childNode" );

            Assert.IsTrue( ManagerContainsNode( sceneManager, childNode ), "A child node was created but not added to the scene graph." );

            node.RemoveAndDestroyChild( childNode );

            Assert.IsFalse( ManagerContainsNode( sceneManager, childNode ), "A child node was destroryed but not removed from the scene graph." );
        }

        private static bool ManagerContainsNode( SceneManager sceneManager, SceneNode childNode )
        {
            bool managerContainsChild = false;

            foreach ( SceneNode sceneNode in sceneManager.SceneNodes )
            {
                if ( sceneNode.Equals( childNode ) )
                {
                    managerContainsChild = true;
                }
            }
            return managerContainsChild;
        }
    }
}
