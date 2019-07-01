using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.DirectX.DirectInput;
using BulletSharp;
using BulletSharp.Math;
using BulletSharp.SoftBody;
using TGC.Core.BulletPhysics;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Geometry;
using TGC.Core.Textures;
using TGC.Core.Particle;
using TGC.Core.Sound;
using TGC.Core.BoundingVolumes;

namespace TGC.Group.Model
{
    public class Auto
    {
        public FisicaMundo Fisica { get; set; }
        public string MediaDir { get; set; }

        //private TgcMesh MayaAuto { get; set; }
        public List<TgcMesh> Mayas { get; set; }
        public RigidBody CuerpoRigidoAuto { get; set; }
        public TGCVector3 PosicionInicial { get; set; }

        //Variables de Mayas de Ruedas
        public List<TgcMesh> Ruedas { get; set; }
        public TgcMesh RuedaDelIzq { get; set; }
        public TgcMesh RuedaDelDer { get; set; }
        public TgcMesh RuedaTrasIzq { get; set; }
        public TgcMesh RuedaTrasDer { get; set; }
        public static TGCVector3 PosicionRuedaDelDer = new TGCVector3(-26, 10.5f, -45f);
        public static TGCVector3 PosicionRuedaDelIzq = new TGCVector3(26, 10.5f, -45f);
        public static TGCVector3 PosicionRuedaTrasDer = new TGCVector3(-26, 10.5f, 44);
        public static TGCVector3 PosicionRuedaTrasIzq = new TGCVector3(26, 10.5f, 44);

        public float GradosRuedaAlDoblar { get; set; }
        public TGCVector3 VersorDirector { get; set; }

        //Cosas de Giros
        public int Direccion { get; set; }
        public TGCVector3 DireccionInicial { get; set; }
        public float GradosRotacion { get; set; }
        public float gradosGiro = FastMath.ToRad(0.4f);

        //Friccion del auto
        public float FriccionAuto { get; set; }

        //Calculo de la Velocidad del Auto
        public float Velocidad
        {
            get => (FastMath.Abs(CuerpoRigidoAuto.LinearVelocity.X) + FastMath.Abs(CuerpoRigidoAuto.LinearVelocity.Z)) * Direccion;
        }
        public float Velocidad2 { get; set; }

        public readonly float AlturaCuerpoRigido = 20f;

        //Movimiento
        public TGCMatrix Movimiento { get => new TGCMatrix(CuerpoRigidoAuto.InterpolationWorldTransform) * TGCMatrix.Translation(1, -AlturaCuerpoRigido, 1); }

        //Matrices Sombra
        public TGCMatrix MovimientoSombra { get => new TGCMatrix(CuerpoRigidoAuto.InterpolationWorldTransform) * TGCMatrix.Translation(1, -CuerpoRigidoAuto.CenterOfMassPosition.Y + 0.05f, 1); }
        public float EscaladoSombra { get => 1 + (CuerpoRigidoAuto.CenterOfMassPosition.Y - AlturaCuerpoRigido) / 100; }
        public TGCMatrix EscalaSombra { get => TGCMatrix.Scaling(EscaladoSombra, 0, EscaladoSombra); }
        public TGCMatrix MovimientoTotalSombra { get => EscalaSombra * MovimientoSombra; }

        //Matriz que rota las rueda izquierda, para que quede como una rueda derecha
        public TGCMatrix FlipRuedaDerecha = TGCMatrix.RotationZ(FastMath.ToRad(180));

        //Matrices que colocan a las ruedas en su lugar
        public TGCMatrix TraslacionRuedaTrasDer = TGCMatrix.Translation(PosicionRuedaTrasDer);
        public TGCMatrix TraslacionRuedaDelDer = TGCMatrix.Translation(PosicionRuedaDelDer);
        public TGCMatrix TraslacionRuedaTrasIzq = TGCMatrix.Translation(PosicionRuedaTrasIzq);
        public TGCMatrix TraslacionRuedaDelIzq = TGCMatrix.Translation(PosicionRuedaDelIzq);

        //Matriz que hace rotar a las ruedas al doblar
        public TGCMatrix RotarRueda { get => TGCMatrix.RotationY(GradosRuedaAlDoblar * Direccion); }

        //Matrices que hacen girar a las ruedas con la velocidad
        public TGCMatrix GiroAcumuladoIzq = TGCMatrix.Identity;
        public TGCMatrix GiroAcumuladoDer = TGCMatrix.Identity;
        public TGCMatrix GirarRuedaIzq { get => TGCMatrix.RotationX(-Velocidad / 130); }
        public TGCMatrix GirarRuedaDer { get => TGCMatrix.RotationX(Velocidad / 130); }

        //Cosas Sombra
        public TgcPlane PlanoSombra { get; set; }
        public TgcTexture Sombra { get; set; }
        public TgcMesh PlanoSombraMesh { get; set; }

        //Cosas Humo del Auto
        public string PathHumo { get; set; }
        public ParticleEmitter CañoDeEscape1;
        public ParticleEmitter CañoDeEscape2;
        public readonly int CantidadParticulas = 5;
        public TGCVector3 PosicionRelativaCaño1 = new TGCVector3(17, 12, 77);
        public TGCVector3 PosicionRelativaCaño2 = new TGCVector3(-17, 12, 77);

        // BoundingBox para Colisiones
        public TGCBox CuerpoAuto;
        public List<TgcBoundingAxisAlignBox> listBB;
        public TgcBoundingAxisAlignBox BBFinal;
        public TgcBoundingOrientedBox OBBAuto;

        //Sonidos

        public Microsoft.DirectX.DirectSound.Device Sonido { get; set; }




        public Auto(List<TgcMesh> mayas, TGCVector3 posicionInicial, float direccionInicialEnGrados, FisicaMundo fisica, string pathHumo,string mediaDir, Microsoft.DirectX.DirectSound.Device sonido)
        {
            MediaDir = mediaDir;
            Sonido = sonido;
            Fisica = fisica;
            Mayas = mayas;
            PosicionInicial = posicionInicial;
            Sombra = TgcTexture.createTexture(MediaDir + "Textures\\SombraAuto.png");
            var rueda = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Rueda-TgcScene.xml").Meshes[0];
            PathHumo = pathHumo;
            DireccionInicial = new TGCVector3(FastMath.Cos(FastMath.ToRad(direccionInicialEnGrados)), 0, FastMath.Sin(FastMath.ToRad(direccionInicialEnGrados)));

            //Creamos las instancias de cada rueda
            RuedaTrasIzq = rueda.createMeshInstance("Rueda Trasera Izquierda");
            RuedaDelIzq = rueda.createMeshInstance("Rueda Delantera Izquierda");
            RuedaTrasDer = rueda.createMeshInstance("Rueda Trasera Derecha");
            RuedaDelDer = rueda.createMeshInstance("Rueda Delantera Derecha");

            //Armo una lista con las ruedas
            Ruedas = new List<TgcMesh>
            {
                RuedaTrasIzq,
                RuedaDelIzq,
                RuedaTrasDer,
                RuedaDelDer
            };

            //Sombras
            PlanoSombra = new TgcPlane(new TGCVector3(-31.5f, 0.2f, -70), new TGCVector3(65, 0, 140), TgcPlane.Orientations.XZplane, Sombra, 1, 1);
            PlanoSombraMesh = PlanoSombra.toMesh("Sombra");
            PlanoSombraMesh.AutoTransformEnable = false;
            PlanoSombraMesh.AlphaBlendEnable = true;

            // ------------------------------------------------------
            // BoundingBox
            listBB = new List<TgcBoundingAxisAlignBox>(); // Creo una lista de las BB del auto
            foreach(var mesh in Mayas)
            {
                listBB.Add(mesh.BoundingBox);
            }

            //
            BBFinal = TgcBoundingAxisAlignBox.computeFromBoundingBoxes(listBB); // Creo una BB a partir de todas las BBs del auto

            // -------------------------------------------------------
            // Humo 
            // Se puede hacer que cambie la textura si acelera, etc
            TGCVector3 VelocidadParticulas = new TGCVector3(10, 5, 10); // La velocidad que se mueve sobre cada eje
            CañoDeEscape1 = new ParticleEmitter(PathHumo, CantidadParticulas)
            {
                Dispersion = 3,
                MaxSizeParticle = 1f,
                MinSizeParticle = 1f,
                Speed = VelocidadParticulas
            };
            CañoDeEscape2 = new ParticleEmitter(PathHumo, CantidadParticulas)
            {
                Dispersion = 3,
                MaxSizeParticle = 1f,
                MinSizeParticle = 1f,
                Speed = VelocidadParticulas
            };

            //var a = new Microsoft.DirectX.DirectSound.Device();

            //// Encendido
            //Encendido = 
            //{
            //    MinDistance = 80f
            //};

        }

        public void Encendido()
        {
            Tgc3dSound sonar;
            sonar = new Tgc3dSound(MediaDir + "Musica\\Encendido.wav", Mayas[0].Position, Sonido)
            {
                MinDistance = 80f
            };
            sonar.play();
        }
        public void Render(float tiempo)
        {
            foreach (var maya in Mayas)
            {
                maya.AutoTransformEnable = false;
                maya.Transform = Movimiento;
                //maya.Render();
            }

            //Matrices que acumulan los cambios
            GiroAcumuladoIzq *= GirarRuedaIzq;
            GiroAcumuladoDer *= GirarRuedaDer;

            RuedaTrasIzq.Transform = GiroAcumuladoIzq * TraslacionRuedaTrasIzq * Movimiento;
            RuedaTrasDer.Transform = GiroAcumuladoDer * FlipRuedaDerecha * TraslacionRuedaTrasDer * Movimiento;
            RuedaDelIzq.Transform = GiroAcumuladoIzq * RotarRueda * TraslacionRuedaDelIzq * Movimiento;
            RuedaDelDer.Transform = GiroAcumuladoDer * FlipRuedaDerecha * RotarRueda * TraslacionRuedaDelDer * Movimiento;
            foreach (var maya in Ruedas)
            {
                maya.Render();
            }

            VersorDirector = TGCVector3.TransformNormal(DireccionInicial, Movimiento);

            //Sombras
            PlanoSombraMesh.Transform = MovimientoTotalSombra;
            PlanoSombraMesh.Render();

            //BoundingBox
            BBFinal.transform(Movimiento);

            //Humo
            D3DDevice.Instance.ParticlesEnabled = true;
            D3DDevice.Instance.EnableParticles();
            CañoDeEscape1.Position = TGCVector3.TransformCoordinate(PosicionRelativaCaño1, Movimiento);
            CañoDeEscape2.Position = TGCVector3.TransformCoordinate(PosicionRelativaCaño2, Movimiento);
            CañoDeEscape1.render(tiempo);
            CañoDeEscape2.render(tiempo);
        }
        public void Dispose()
        {
            CañoDeEscape1.dispose();
            CañoDeEscape2.dispose();
            PlanoSombraMesh.Dispose();
            foreach (var maya in Ruedas)
            {
                maya.Dispose();
            }
            foreach (var maya in Mayas)
            {
                maya.Dispose();
            }
        }
    }
}
