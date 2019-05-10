using UnityEngine;
using System.Collections;

public class cloth_motion: MonoBehaviour {

	float 		t;
    float t_for_force;
	int[] 		edge_list;
	float 		mass;
	float		damping;
	float 		stiffness;
	float[] 	L0;
	Vector3[] 	velocities;
    Vector3[] acc;
    Vector3 g;
    Vector3[] vertices_new;
    Vector3[] sum_x;
    int[] sum_n;

	// Use this for initialization
	void Start () 
	{
		t 			= 0.075f;
		mass 		= 1.0f;
		damping 	= 0.99f;
        t_for_force = 0.02f;
        stiffness 	= 1000.0f;

        g = new Vector3(0,-9.8f,0);

		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		int[] 		triangles = mesh.triangles;
		Vector3[] 	vertices = mesh.vertices;

        vertices_new = new Vector3[vertices.Length];

        sum_x = new Vector3[vertices.Length];

        sum_n = new int[vertices.Length];

        //Construct the original edge list
        int[] original_edge_list = new int[triangles.Length*2];
		for (int i=0; i<triangles.Length; i+=3) 
		{
			original_edge_list[i*2+0]=triangles[i+0];
			original_edge_list[i*2+1]=triangles[i+1];
			original_edge_list[i*2+2]=triangles[i+1];
			original_edge_list[i*2+3]=triangles[i+2];
			original_edge_list[i*2+4]=triangles[i+2];
			original_edge_list[i*2+5]=triangles[i+0];
		}
		//Reorder the original edge list
		for (int i=0; i<original_edge_list.Length; i+=2)
			if(original_edge_list[i] > original_edge_list[i + 1]) 
				Swap(ref original_edge_list[i], ref original_edge_list[i+1]);
		//Sort the original edge list using quicksort
		Quick_Sort (ref original_edge_list, 0, original_edge_list.Length/2-1);

		int count = 0;
		for (int i=0; i<original_edge_list.Length; i+=2)
			if (i == 0 || 
			    original_edge_list [i + 0] != original_edge_list [i - 2] ||
			    original_edge_list [i + 1] != original_edge_list [i - 1]) 
					count++;

		edge_list = new int[count * 2];
		int r_count = 0;
		for (int i=0; i<original_edge_list.Length; i+=2)
			if (i == 0 || 
			    original_edge_list [i + 0] != original_edge_list [i - 2] ||
				original_edge_list [i + 1] != original_edge_list [i - 1]) 
			{
				edge_list[r_count*2+0]=original_edge_list [i + 0];
				edge_list[r_count*2+1]=original_edge_list [i + 1];
				r_count++;
			}


		L0 = new float[edge_list.Length/2];
		for (int e=0; e<edge_list.Length/2; e++) 
		{
			int v0 = edge_list[e*2+0];
			int v1 = edge_list[e*2+1];
			L0[e]=(vertices[v0]-vertices[v1]).magnitude;
		}

		velocities = new Vector3[vertices.Length];
        
        for (int v=0; v<vertices.Length; v++)
			velocities [v] = new Vector3 (0, 0, 0);

        acc = new Vector3[vertices.Length];
        for (int v = 0; v < vertices.Length; v++)
            acc[v] = new Vector3(0, 0, 0);

        //for(int i=0; i<edge_list.Length/2; i++)
        //	Debug.Log ("number"+i+" is" + edge_list [i*2] + "and"+ edge_list [i*2+1]);
    }

	void Quick_Sort(ref int[] a, int l, int r)
	{
		int j;
		if(l<r)
		{
			j=Quick_Sort_Partition(ref a, l, r);
			Quick_Sort (ref a, l, j-1);
			Quick_Sort (ref a, j+1, r);
		}
	}

	int  Quick_Sort_Partition(ref int[] a, int l, int r)
	{
		int pivot_0, pivot_1, i, j;
		pivot_0 = a [l * 2 + 0];
		pivot_1 = a [l * 2 + 1];
		i = l;
		j = r + 1;
		while (true) 
		{
			do ++i; while( i<=r && (a[i*2]<pivot_0 || a[i*2]==pivot_0 && a[i*2+1]<=pivot_1));
			do --j; while(  a[j*2]>pivot_0 || a[j*2]==pivot_0 && a[j*2+1]> pivot_1);
			if(i>=j)	break;
			Swap(ref a[i*2], ref a[j*2]);
			Swap(ref a[i*2+1], ref a[j*2+1]);
		}
		Swap (ref a [l * 2 + 0], ref a [j * 2 + 0]);
		Swap (ref a [l * 2 + 1], ref a [j * 2 + 1]);
		return j;
	}

	void Swap(ref int a, ref int b)
	{
		int temp = a;
		a = b;
		b = temp;
	}


	void Strain_Limiting()
	{
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        
        vertices_new = new Vector3[vertices.Length];
        for (int v = 0; v < vertices_new.Length; v++)
            vertices_new[v] = new Vector3(0, 0, 0);
        
        sum_x = new Vector3[vertices.Length];
        for (int v = 0; v < sum_x.Length; v++)
            sum_x[v] = new Vector3(0, 0, 0);

        sum_n = new int[vertices.Length];
        for (int v = 0; v < sum_n.Length; v++)
            sum_n[v] = 0;

        for (int e = 0; e < edge_list.Length / 2; e++)
        {
            int i = edge_list[e * 2 + 0];
            int j = edge_list[e * 2 + 1];

            //Desired position of vertex i and j
            Vector3 xie_new = 0.5f * (vertices[i] + vertices[j] + (vertices[i] - vertices[j]) * L0[e] / (vertices[i] - vertices[j]).magnitude);
            Vector3 xje_new = 0.5f * (vertices[j] + vertices[i] + (vertices[j] - vertices[i]) * L0[e] / (vertices[j] - vertices[i]).magnitude);

            sum_x[i] += xie_new;
            sum_x[j] += xje_new;
            sum_n[i]++;
            sum_n[j]++;
        }

        for(int i = 0; i < vertices_new.Length; i++)
        {
            if (i != 0 && i != 10)
                vertices_new[i] = (0.2f * vertices[i] + sum_x[i]) / (0.2f + sum_n[i]);
            else
                vertices_new[i] = vertices[i];
        }

        for(int i = 0; i < velocities.Length; i++)
        {
            velocities[i] = velocities[i] + (vertices_new[i] - vertices[i]) / t;
        }
        mesh.vertices = vertices_new;
        mesh.RecalculateNormals();
    }


	void Collision_Handling()
	{
        
        GameObject sphere = GameObject.Find("Sphere");
        float R = sphere.transform.localScale.x/2 + 0.5f;
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        //Mesh sphere_mesh = sphere.GetComponent<MeshFilter>().mesh;
        //float R = sphere_mesh.bounds.extents.magnitude;

        //Vector3 center_pos = sphere.transform.TransformPoint(sphere.transform.position);
        Vector3 center_pos = transform.TransformPoint(sphere.transform.position);
        
        for (int i = 0; i < vertices_new.Length; i++)
        {
            if((vertices_new[i]-center_pos).magnitude <= R)
            {
                
                vertices_new[i] = center_pos + (vertices_new[i] - center_pos) * R / (vertices_new[i] - center_pos).magnitude;
            }
        }
        for (int i = 0; i < velocities.Length; i++)
        {
            if(i!=0&&i!=10)
            velocities[i] = velocities[i] + (vertices_new[i] - vertices[i]) / t;
        }
        mesh.vertices = vertices_new;
        mesh.RecalculateNormals();

    }

    void Spring_force()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] sum_force = new Vector3[acc.Length];
        for (int e = 0; e < edge_list.Length / 2; e++)
        {
            int i = edge_list[e * 2 + 0];
            int j = edge_list[e * 2 + 1];
            
            //Force for vertex i and hj
            Vector3 i_force = -stiffness * ((vertices[i] - vertices[j]).magnitude - L0[e]) * (vertices[i] - vertices[j]) / (vertices[i] - vertices[j]).magnitude;
            Vector3 j_force = -i_force;
            sum_force[i] += i_force;
            sum_force[j] += j_force;
        }
        //Calculate acceleration for each vertex
        for(int i = 0; i < acc.Length; i++)
        {
            acc[i] = (sum_force[i] / mass);
            velocities[i] = velocities[i] + t_for_force * acc[i];
        }

        //Update velocities and positions
        for (int i = 0; i < vertices.Length; i++)
        {
            if (i != 0 && i != 10)
            {
                velocities[i] = velocities[i] + t_for_force * g;
                velocities[i] = damping * velocities[i];
                vertices[i] = vertices[i] + t_for_force * velocities[i];
            }
            
        }

        mesh.vertices = vertices;

        

    }

	// Update is called once per frame
	void Update () 
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3[] vertices = mesh.vertices;

        //If you want to check Spring_force function, please comment Strain_limiting area below  
        //and uncomment the Strain_force area.

        //Strain_limiting method
        //----------------------------------------------------------------------------------------------------

        vertices_new = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            if(i==0 || i == 10)
            {
                
            }
            else
            {
                velocities[i] = velocities[i] + t * g;
                velocities[i] = velocities[i] * damping;
                //Debug.Log("Before Update: " + velocities[i]);
                Vector3 temp_pos = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                temp_pos = temp_pos + velocities[i] * t;
                
                vertices[i].Set(temp_pos.x, temp_pos.y, temp_pos.z);
            }
        }
        mesh.vertices = vertices;

        mesh.RecalculateNormals();

        for (int i = 0; i < 50; i++)
            Strain_Limiting();

        Collision_Handling();


        //----------------------------------------------------------------------------------------------------



        //If you want to check Spring_force function, please comment Strain_limiting area above  
        //and uncomment the area below

        /*
         *      For the problem mentioned in bonus credit part, I notice that v(t+1)=v(t)+a*t
         *  and a = stiffness * (L-L0), x(t+1)= x(t)+v(t)*t. Therefore, both a large stiffness
         *  and a large t can cause huge change in position, in other word, numerical instability
         *  will happen when both of stiffness and t are large. When stiffness = 1000, t = 0.075
         *  becomes a large number that can cause numerical instability. Therefore, t =0.02 is an
         *  apporiate interval for stiffness = 1000.
         *      What I use is t = 0.02s and stiffness = 1000
         * 
         */
        //Strain force area
        /*
        //----------------------------------------------------------------------------------------------------
        Spring_force();
        vertices_new = mesh.vertices;
        Collision_Handling();
        

        mesh.RecalculateNormals();
        //----------------------------------------------------------------------------------------------------
        */
    }
}
