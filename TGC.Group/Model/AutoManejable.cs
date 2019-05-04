using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;


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
        private float direccionInicial;
        public float DireccionInicial { get => FastMath.ToRad(270); set => direccionInicial = value; }
        private float Aceleracion { get; set; }
        public bool VelocidadesCriticas { get => Velocidad < 0.05f && Velocidad > -0.05f; }
        public int Direccion { get; set; }
        public float Grados { get; set; }
        public float VelocidadInicial { get; set; }
        private float velocidad;
        public float Velocidad
        {
            get => FastMath.Min(FastMath.Max(VelocidadInicial + Aceleracion * Direccion, velocidadMinima), velocidadMaxima);
            set => velocidad = value;
        }

        public TGCVector3 VersorDirector()
        {
            return new TGCVector3(FastMath.Cos(DireccionInicial + Grados), 0, FastMath.Sin(DireccionInicial + Grados));
        }

        public float giroTotal()
        {
            return gradosGiro * (Velocidad / 10);
        }

        //Movimiento
        public void Acelera()
        {
            if (Velocidad >= 0)
            {
                Aceleracion += 0.02f;
                Direccion = 1;
            }
        }
        public void Frena()
        {

            if (VelocidadesCriticas)
            {
                this.Parado();
            }
            else
            {
                Aceleracion -= 0.1f;
            }
        }
        public void MarchaAtras()
        {
            if (Velocidad <= 0)
            {
                Aceleracion += 0.02f;
                Direccion = -1;
            }
            else
            {
                this.Parado();
            }
        }
        public void GiraDerecha()
        {
            Grados -= this.giroTotal();
            Maya.RotateY(+giroTotal());
        }
        public void GiraIzquierda()
        {
            Grados += this.giroTotal();
            Maya.RotateY(-giroTotal());
        }
        public void Parado()
        {
            VelocidadInicial = Velocidad;
            Maya.RotateY(0);
            Aceleracion = 0;
            if (Velocidad != 0)
            {
                if (VelocidadesCriticas)
                {
                    VelocidadInicial = 0;
                }
                else
                {
                    Aceleracion -= 0.008f;
                }
            }
        }
        //public TGCMatrix Traslacion { get => TGCMatrix.Translation(VersorDirector().X * Velocidad, 0, VersorDirector().Z * Velocidad); }
        //public TGCMatrix Rotacion {  get => TGCMatrix.RotationY(this.giroTotal()); }
        //public TGCMatrix Movimiento { get => Traslacion * Rotacion; }
        public void Moverse()
        { 
            //maya.Transform = Traslacion * Rotacion;
            maya.Move(VersorDirector()* Velocidad);
        }
    }



}

