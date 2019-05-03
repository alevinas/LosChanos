using Microsoft.DirectX.DirectInput;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Camara;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;

namespace TGC.Group.Model
{
    public class AutoManejable
    {
        private TgcMesh maya;
        public TgcMesh Maya { get => maya; set => maya = value; }
        public AutoManejable(TgcMesh valor)
        {
            maya = valor;
        }
        public float gradosGiro = 0.017f;
        public float velocidadMinima = -2;
        public float velocidadMaxima = 13;
        private float Aceleracion { get; set; }
        public int Direccion { get; set; }
        public float Grados { get; set; }
        private float velocidad;
        public float Velocidad
        {
            get => FastMath.Min(FastMath.Max(Aceleracion * Direccion, velocidadMinima), velocidadMaxima);
            set => velocidad = value;
        }

        public TGCVector3 versorDirector()
        {
            return new TGCVector3(FastMath.Cos(4.71238898f + Grados), 0, FastMath.Sin(4.71238898f + Grados));
        }

        public float giroTotal()
        {
            return gradosGiro * (Velocidad / 10);
        }

        //Movimiento
        public void acelera()
        {
            Aceleracion += 0.02f;
            Direccion = 1;
        }
        public void frena()
        {
            Aceleracion -= 0.1f;
            if (Velocidad < 1f)
            {
                Aceleracion = 0;
            }
        }
        public void marchaAtras()
        {
            Aceleracion += 0.02f;
            Direccion = -1;        
        }
        public void giraDerecha()
        {
            Grados -= this.giroTotal();
            Maya.RotateY(+giroTotal());
        }
        public void giraIzquierda()
        {
            Grados += this.giroTotal();
            Maya.RotateY(-giroTotal());
        }
        public void parado()
        {
            Maya.RotateY(0);
            if (Aceleracion != 0)
            {
                if (Aceleracion > 0.05f)
                {
                    Aceleracion -= 0.008f;
                }
                else if (Aceleracion < -0.05f)
                {
                    Aceleracion += 0.008f;
                }
                else
                {
                    Aceleracion = 0;
                }
            }
        }
        public void moverse()
        {
            maya.Move(this.versorDirector() * Velocidad);
        }
    }



}


