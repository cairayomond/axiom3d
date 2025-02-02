#region MIT/X11 License

//Copyright � 2003-2012 Axiom 3D Rendering Engine Project
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

using System;

namespace Axiom.Samples
{
	/// <summary>
	/// Occurs when a button widget was hit.
	/// </summary>
	/// <param name="button"></param>
	public delegate void ButtonHitDelegate( object sender, Button button );

	/// <summary>
	/// Listener class for responding to tray events.
	/// </summary>
	public interface ISdkTrayListener
	{
		/// <summary>
		/// Occurs when a button widget was hit.
		/// </summary>
		event ButtonHitDelegate ButtonHit;

		void OnButtonHit( object sender, Button button );
		void ItemSelected( SelectMenu menu );
		void LabelHit( Label label );
		void SliderMoved( Slider slider );
		void CheckboxToggled( CheckBox box );
		void OkDialogClosed( String message );
		void YesNoDialogClosed( String question, bool yesHit );
	};
}