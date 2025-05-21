public class Tetrahedron
{
    public int[] NodeIndices { get; }

    public Tetrahedron(params int[] indices)
    {
        if (indices.Length != 4)
            throw new System.ArgumentException("Tetrahedron requires exactly 4 node indices");
        
        NodeIndices = indices;
    }
}
