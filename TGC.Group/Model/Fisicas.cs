using System;
using BulletSharp;
using System.Collections.Generic;
using TGC.Core.BulletPhysics;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;

namespace TGC.Group.Model
{   
   public class FisicaMundo
    {
        private DiscreteDynamicsWorld dynamicsWorld;
        private CollisionDispatcher dispatcher;
        private DefaultCollisionConfiguration collisionConfiguration;
        private SequentialImpulseConstraintSolver constraintSolver;
        private BroadphaseInterface overlappingPairCache;

        private List<TgcMesh> meshes = new List<TgcMesh>();
        private RigidBody piso;
        private TgcMesh auto;
        private RigidBody cuerpoAuto;
        private TGCVector3 adelante;
        private TGCVector3 izquierda_derecha;

        public void cargarEdificios(List<TgcMesh> meshes)
        {
            this.meshes = meshes;
        }

        public virtual void Init(string MediaDir)
        {
            collisionConfiguration = new DefaultCollisionConfiguration();
            dispatcher = new CollisionDispatcher(collisionConfiguration);
            GImpactCollisionAlgorithm.RegisterAlgorithm(dispatcher);
            constraintSolver = new SequentialImpulseConstraintSolver();
            overlappingPairCache = new DbvtBroadphase(); //AxisSweep3(new BsVector3(-5000f, -5000f, -5000f), new BsVector3(5000f, 5000f, 5000f), 8192);
            dynamicsWorld = new DiscreteDynamicsWorld(dispatcher, overlappingPairCache, constraintSolver, collisionConfiguration);
            dynamicsWorld.Gravity = new TGCVector3(0, -100f, 0).ToBulletVector3();

            foreach (var mesh in meshes)
            {
                var buildingbody = BulletRigidBodyFactory.Instance.CreateRigidBodyFromTgcMesh(mesh);
                dynamicsWorld.AddRigidBody(buildingbody);
            }
        
            var cuerpoPiso = new StaticPlaneShape(TGCVector3.Up.ToBulletVector3(), 10);
            cuerpoPiso.LocalScaling = new TGCVector3().ToBulletVector3();
            var movimientoPiso = new DefaultMotionState();
            var pisoConstruccion = new RigidBodyConstructionInfo(0, movimientoPiso, cuerpoPiso);
            piso = new RigidBody(pisoConstruccion);
            piso.Friction = 1;
            piso.RollingFriction = 1;
            piso.Restitution = 1f;
            piso.UserObject = "floorBody";
            dynamicsWorld.AddRigidBody(piso);
        }

        public void Update(TgcD3dInput input)
        {
            dynamicsWorld.StepSimulation(1 / 60f, 100);
        }

        public void Render(float tiempo)
        {
            //Hacemos render de la escena.
            foreach (var mesh in meshes) mesh.Render();

            //Se hace el transform a la posicion que devuelve el el Rigid Body del Hummer
            //hummer.Position = new TGCVector3(hummerBody.CenterOfMassPosition.X, hummerBody.CenterOfMassPosition.Y + 0, hummerBody.CenterOfMassPosition.Z);
            //hummer.Transform = TGCMatrix.Translation(hummerBody.CenterOfMassPosition.X, hummerBody.CenterOfMassPosition.Y, hummerBody.CenterOfMassPosition.Z);
            //hummer.Render();
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
            foreach (TgcMesh mesh in meshes)
            {
                mesh.Dispose();
            }

        }
    }
}
