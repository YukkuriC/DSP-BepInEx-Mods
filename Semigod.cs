

namespace DSPMod
{
    public static class Semigod
    {
        public static void FinishDyson()
        {
            DysonSphere target = null;
            foreach (var ds in GameMain.data.dysonSpheres)
            {
                if (ds != null && ds.starData == GameMain.localStar)
                    target = ds;
            }
            if (target == null) return;

            foreach (var layer in target.layersIdBased)
            {
                if (layer == null) continue;
                foreach (var node in layer.nodePool)
                {
                    if (node == null) continue;
                    while (node.spReqOrder > 0)
                        target.ConstructSp(node);
                    while (node.cpReqOrder > 0)
                        node.ConstructCp();
                }
            }
        }
    }
}
