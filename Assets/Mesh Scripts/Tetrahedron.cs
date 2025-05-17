public class Tetrahedron
{
    public int Id;
    public int[] NodeIndices;

    public Tetrahedron(int id, int n0, int n1, int n2, int n3)
    {
        Id = id;
        NodeIndices = new int[] { n0, n1, n2, n3 };
    }
}
