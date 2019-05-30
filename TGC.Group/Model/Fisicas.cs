using System;
using Microsoft.DirectX.DirectInput;
using BulletSharp;
using System.Collections.Generic;
using TGC.Core.BulletPhysics;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Core.Geometry;


namespace TGC.Group.Model
{
    public class FisicaMundo
    {
        //Declaro Iniciales
        private DiscreteDynamicsWorld dynamicsWorld;
        private CollisionDispatcher dispatcher;
        private DefaultCollisionConfiguration collisionConfiguration;
        private SequentialImpulseConstraintSolver constraintSolver;
        private BroadphaseInterface overlappingPairCache;

        //Escenario
        private List<TgcMesh> Edificios = new List<TgcMesh>();
        private RigidBody piso;
        public void CargarEdificios(List<TgcMesh> meshes)
        {
            Edificios = meshes;
        }

        //private TgcMesh MayaAuto { get; set; }
        private List<TgcMesh> Mayas { get; set; }
        private RigidBody CuerpoRigidoAuto { get; set; }

        //Direcciones
        private TGCVector3 adelante;
        private TGCVector3 izquierda_derecha;

        //Teclas
        private Key Acelerar { get; set; }
        private Key Atras { get; set; }
        private Key Derecha { get; set; }
        private Key Izquierda { get; set; }

        //Variables de Mayas de Ruedas
        public List<TgcMesh> Ruedas { get; set; }
        public TgcMesh RuedaDelIzq { get; set; }
        public TgcMesh RuedaDelDer { get; set; }
        public TgcMesh RuedaTrasIzq { get; set; }
        public TgcMesh RuedaTrasDer { get; set; }

        public TGCVector3 PosicionRuedaDelDer = new TGCVector3(-26, 10.5f, -45f);
        public TGCVector3 PosicionRuedaDelIzq = new TGCVector3(26, 10.5f, -45f);
        public TGCVector3 PosicionRuedaTrasDer = new TGCVector3(-26, 10.5f, 44);
        public TGCVector3 PosicionRuedaTrasIzq = new TGCVector3(26, 10.5f, 44);

        public virtual void Init(List<TgcMesh> valor,TgcMesh rueda)
        {
            Mayas = valor;

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

            //Implementación Iniciales
            collisionConfiguration = new DefaultCollisionConfiguration();
            dispatcher = new CollisionDispatcher(collisionConfiguration);
            GImpactCollisionAlgorithm.RegisterAlgorithm(dispatcher);
            constraintSolver = new SequentialImpulseConstraintSolver();
            overlappingPairCache = new DbvtBroadphase(); //AxisSweep3(new BsVector3(-5000f, -5000f, -5000f), new BsVector3(5000f, 5000f, 5000f), 8192);
            dynamicsWorld = new DiscreteDynamicsWorld(dispatcher, overlappingPairCache, constraintSolver, collisionConfiguration);
            dynamicsWorld.Gravity = new TGCVector3(0, -100f, 0).ToBulletVector3();

            //Se hacen los cuerpos rígidos del escenario.
            foreach (var mesh in Edificios)
            {
                var objetos = BulletRigidBodyFactory.Instance.CreateRigidBodyFromTgcMesh(mesh);
                dynamicsWorld.AddRigidBody(objetos);
            }

            // Estructura del piso
            var cuerpoPiso = new StaticPlaneShape(TGCVector3.Up.ToBulletVector3(), 10);
            cuerpoPiso.LocalScaling = new TGCVector3().ToBulletVector3();
            var movimientoPiso = new DefaultMotionState();
            var pisoConstruccion = new RigidBodyConstructionInfo(0, movimientoPiso, cuerpoPiso);
            piso = new RigidBody(pisoConstruccion);
            piso.Friction = 0.0001f;
            piso.RollingFriction = 1;
            piso.Restitution = 1f;
            piso.UserObject = "floorBody";
            dynamicsWorld.AddRigidBody(piso);

            //Estructura del auto (Hacemos como una caja con textura)
            //var loader = new TgcSceneLoader();

            //TgcTexture texture = TgcTexture.createTexture(D3DDevice.Instance.Device, MediaDir + @"Textures\box4.jpg");
            //TGCBox boxMesh1 = TGCBox.fromSize(new TGCVector3(20, 20, 20), texture);
            //TGCBox boxMesh1 = TGCBox.fromSize(new TGCVector3(0, 0, 0), new TGCVector3(10, 1, 10));
            //MayaAuto = boxMesh1.ToMesh("box");
            //boxMesh1.Dispose();

            var tamañoAuto = new TGCVector3(500,1, 40);
            CuerpoRigidoAuto = BulletRigidBodyFactory.Instance.CreateBox(tamañoAuto, 10, new TGCVector3(0,0,0) , 0, 0, 0, 0.55f, true);
            CuerpoRigidoAuto.Restitution = 0.2f;
            CuerpoRigidoAuto.Gravity = new TGCVector3(0, -100f, 0).ToBulletVector3();
            dynamicsWorld.AddRigidBody(CuerpoRigidoAuto);


            foreach (var maya in Mayas)
            {
               maya.Position = TGCVector3.Empty;
            }
            

            //Vectores de la direccion del auto post-choque
            adelante = new TGCVector3(0, 0, 1);
            izquierda_derecha = new TGCVector3(1, 0, 0);
        }

        private void ComportamientoFisico(BulletSharp.Math.Vector3 impulso)
        {
            CuerpoRigidoAuto.ActivationState = ActivationState.ActiveTag;
            CuerpoRigidoAuto.AngularVelocity = TGCVector3.Empty.ToBulletVector3();
            CuerpoRigidoAuto.ApplyCentralImpulse(impulso);
        }

        public void ConfigurarTeclas(Key acelerar, Key atras, Key derecha, Key izquierda)
        {
            Acelerar = acelerar;
            Atras = atras;
            Derecha = derecha;
            Izquierda = izquierda;
        }

        public void Update(TgcD3dInput input)
        {
            var fuerza = 30.30f;
            dynamicsWorld.StepSimulation(1 / 60f, 100);
        
            if (input.keyDown(Acelerar))
            {
                ComportamientoFisico(-fuerza * adelante.ToBulletVector3());
            }
            else if(input.keyDown(Izquierda))
            {
                ComportamientoFisico(fuerza * izquierda_derecha.ToBulletVector3());
            }
            else if(input.keyDown(Derecha))
            {
                ComportamientoFisico(-fuerza * izquierda_derecha.ToBulletVector3());
            }
            else if(input.keyDown(Atras))
            {
                ComportamientoFisico(fuerza * adelante.ToBulletVector3());
            }

        }


        //Matriz que rota las rueda izquierda, para que quede como una rueda derecha
        public TGCMatrix FlipRuedaDerecha { get => TGCMatrix.RotationZ(FastMath.ToRad(180)); }

        //Matrices que colocan a las ruedas en su lugar
        public TGCMatrix TraslacionRuedaTrasDer { get => TGCMatrix.Translation(PosicionRuedaTrasDer); }
        public TGCMatrix TraslacionRuedaDelDer { get => TGCMatrix.Translation(PosicionRuedaDelDer); }
        public TGCMatrix TraslacionRuedaTrasIzq { get => TGCMatrix.Translation(PosicionRuedaTrasIzq); }
        public TGCMatrix TraslacionRuedaDelIzq { get => TGCMatrix.Translation(PosicionRuedaDelIzq); }


        public void Render(float tiempo)
        {
            //Hacemos render de la escena.
            foreach (var mesh in Edificios) mesh.Render();



            foreach (var maya in Mayas)
            {
                maya.Position = new TGCVector3(CuerpoRigidoAuto.CenterOfMassPosition.X, CuerpoRigidoAuto.CenterOfMassPosition.Y , CuerpoRigidoAuto.CenterOfMassPosition.Z);
                maya.Transform = TGCMatrix.Translation(CuerpoRigidoAuto.CenterOfMassPosition.X, CuerpoRigidoAuto.CenterOfMassPosition.Y, CuerpoRigidoAuto.CenterOfMassPosition.Z);
                maya.Render();
            }
            RuedaTrasIzq.Transform = TraslacionRuedaTrasIzq * TGCMatrix.Translation(CuerpoRigidoAuto.CenterOfMassPosition.X, CuerpoRigidoAuto.CenterOfMassPosition.Y, CuerpoRigidoAuto.CenterOfMassPosition.Z);
            RuedaTrasDer.Transform = FlipRuedaDerecha * TraslacionRuedaTrasDer * TGCMatrix.Translation(CuerpoRigidoAuto.CenterOfMassPosition.X, CuerpoRigidoAuto.CenterOfMassPosition.Y, CuerpoRigidoAuto.CenterOfMassPosition.Z);
            RuedaDelIzq.Transform = TraslacionRuedaDelIzq * TGCMatrix.Translation(CuerpoRigidoAuto.CenterOfMassPosition.X, CuerpoRigidoAuto.CenterOfMassPosition.Y, CuerpoRigidoAuto.CenterOfMassPosition.Z);
            RuedaDelDer.Transform = FlipRuedaDerecha* TraslacionRuedaDelDer * TGCMatrix.Translation(CuerpoRigidoAuto.CenterOfMassPosition.X, CuerpoRigidoAuto.CenterOfMassPosition.Y, CuerpoRigidoAuto.CenterOfMassPosition.Z);
            foreach (var maya in Ruedas)
            {
                maya.Render();
            }
        }

        public void Dispose()
        {
            //Se hace dispose del modelo fisico.
            dynamicsWorld.Dispose();
            dispatcher.Dispose();
            collisionConfiguration.Dispose();
            constraintSolver.Dispose();
            overlappingPairCache.Dispose();
            piso.Dispose();

            //Dispose de Meshes
            foreach (var maya in Edificios)
            {
                maya.Dispose();
            }
        }
    }
}
