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

        public virtual void Init(string MediaDir)
        {
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
            var loader = new TgcSceneLoader();

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

            Mayas = loader.loadSceneFromFile(MediaDir + "AutoPolicia-TgcScene.xml").Meshes;

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

        public void Update(TgcD3dInput input)
        {
            var fuerza = 30.30f;
            dynamicsWorld.StepSimulation(1 / 60f, 100);
        
            if (input.keyDown(Key.UpArrow))
            {
                ComportamientoFisico(-fuerza * adelante.ToBulletVector3());
            }
            else if(input.keyDown(Key.LeftArrow))
            {
                ComportamientoFisico(fuerza * izquierda_derecha.ToBulletVector3());
            }
            else if(input.keyDown(Key.RightArrow))
            {
                ComportamientoFisico(-fuerza * izquierda_derecha.ToBulletVector3());
            }
            else if(input.keyDown(Key.DownArrow))
            {
                ComportamientoFisico(fuerza * adelante.ToBulletVector3());
            }

        }

        public void Render(float tiempo)
        {
            //Hacemos render de la escena.
            foreach (var mesh in Edificios) mesh.Render();

            //Se hace el transform a la posicion que devuelve el el Rigid Body del Hummer

            foreach (var maya in Mayas)
            {
                maya.Position = new TGCVector3(CuerpoRigidoAuto.CenterOfMassPosition.X, CuerpoRigidoAuto.CenterOfMassPosition.Y , CuerpoRigidoAuto.CenterOfMassPosition.Z);
                maya.Transform = TGCMatrix.Translation(CuerpoRigidoAuto.CenterOfMassPosition.X, CuerpoRigidoAuto.CenterOfMassPosition.Y, CuerpoRigidoAuto.CenterOfMassPosition.Z);
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
