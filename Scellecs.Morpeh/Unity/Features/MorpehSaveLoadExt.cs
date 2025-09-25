using Scellecs.Morpeh;
using System.IO;
using UnityEngine;

public static class MorpehSaveLoadExt {
    public static void Write(this BinaryWriter w, Vector3 vector) {
        w.Write(vector.x);
        w.Write(vector.y);
        w.Write(vector.z);
    }
    
    public static Vector3 ReadVector3(this BinaryReader r) {
        return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    }
    
    public static void Write(this BinaryWriter w, Entity entity) {
        w.Write(entity.Id);
    }

    public static Entity ReadEntity(this BinaryReader r) {
        return World.Default.GetEntity(r.ReadInt32());
    }
}
