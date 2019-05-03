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
        
        public TgcMesh Maya { get; set; }
        public AutoManejable(TgcMesh valor)
        {
            Maya = valor;
        }

        public float gradosGiro = 0.017f;
        public float velocidadMinima = -2;
        public float velocidadMaxima = 13;

        private float aceleracion = 0;
        public float Aceleracion { get => aceleracion; set => aceleracion = value; }
        private float tiempoBotonApretado = 0.0f;
        public float TiempoBotonApretado { get => tiempoBotonApretado; set => tiempoBotonApretado = value; }
        private int direccion = 1;
        public int Direccion { get => direccion; set => direccion = value; }
        private float rozamiento = 0.005f;
        public float Rozamiento { get => rozamiento; set => rozamiento = value; }

        public float Grados { get; set; }
        private float velocidad = 0;
        public float Velocidad
        {
            get => FastMath.Min(FastMath.Max((velocidad + (aceleracion * tiempoBotonApretado) - (rozamiento * velocidad)), velocidadMinima), velocidadMaxima);
            set => velocidad = value;
        }

        public TGCVector3 versorDirector()
        {
            return new TGCVector3(FastMath.Cos(4.71238898f + Grados), 0, FastMath.Sin(4.71238898f + Grados));
        }

        public float giroTotal()
        {
            return gradosGiro * (velocidad / 10);
        }

        //Movimiento
        public void acelera()
        {
            aceleracion += 0.02f;
            direccion = 1;
        }
        public void frena()
        {
            rozamiento += 0.010f;
            if (velocidad < 0.05f)
            {
                velocidad = 0;
            }
        }
        public void marchaAtras()
        {
            aceleracion -= 0.02f;
            direccion = -1;
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
            aceleracion = 0;
            rozamiento = 0.005f;
        }
        public void moverse()
        {
            Maya.Move(this.versorDirector() * velocidad);
        }
    }



}


