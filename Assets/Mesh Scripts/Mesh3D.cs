using System.Collections.Generic;

public class Mesh3D
{
    public List<Node> Nodes = new List<Node>();
    public List<Tetrahedron> Elements = new List<Tetrahedron>();

    public void GenerateSimpleCubeMesh()
    {
        Nodes.Add(new Node(0, 0, 0, 0));
        Nodes.Add(new Node(1, 1, 0, 0));
        Nodes.Add(new Node(2, 1, 1, 0));
        Nodes.Add(new Node(3, 0, 1, 0));
        Nodes.Add(new Node(4, 0, 0, 1));
        Nodes.Add(new Node(5, 1, 0, 1));
        Nodes.Add(new Node(6, 1, 1, 1));
        Nodes.Add(new Node(7, 0, 1, 1));

        Elements.Add(new Tetrahedron(0, 0, 1, 3, 4));
        Elements.Add(new Tetrahedron(1, 1, 2, 3, 6));
        Elements.Add(new Tetrahedron(2, 1, 3, 4, 6));
        Elements.Add(new Tetrahedron(3, 3, 4, 6, 7));
        Elements.Add(new Tetrahedron(4, 1, 4, 5, 6));
    }
}
