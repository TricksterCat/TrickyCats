namespace GameRules.Scripts.ECS.Render
{
    public class RenderConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnitRenderProxy component) => { component.Convert(GetPrimaryEntity(component), DstEntityManager, this); });
        }
    }
}
