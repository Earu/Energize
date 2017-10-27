using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Utils
{
    class ImagePoint
    {
        private int _X;
        private int _Y;

        public ImagePoint(int x,int y)
        {
            this._X = x;
            this._Y = y;
        }

        public int X { get => this._X; set => this._X = value; }
        public int Y { get => this._Y; set => this._Y = value; }
    }
}
