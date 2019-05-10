using UnityEngine;
using System.Collections;

public class Rigid_Bunny : MonoBehaviour 
{
	public bool launched = false;
	
	public Vector3 v;							// velocity
	public Vector3 w=new Vector3(0, 0, 0);		// angular velocity
	
	public float m;
	public float mass;							// mass
	public Matrix4x4 I_body;					// body inertia

	public float linear_damping;				// for damping
	public float angular_damping;
	public float restitution;					// for collision
    public Quaternion q = Quaternion.identity;
    private bool ifHasG = false;


	// Use this for initialization
	void Start () 
	{
		//Initialize coefficients
		w = new Vector3 (0, 40.0f, 0);
		linear_damping  = 0.999f;
		angular_damping = 0.98f;
		restitution 	= 0.5f;		//elastic collision
		m 				= 1;
		mass 			= 0; 
		
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] vertices = mesh.vertices;
		for (int i=0; i<vertices.Length; i++) 
		{
			mass += m;
			float diag=m*vertices[i].sqrMagnitude;
			I_body[0, 0]+=diag;
			I_body[1, 1]+=diag;
			I_body[2, 2]+=diag;
			I_body[0, 0]-=m*vertices[i][0]*vertices[i][0];
			I_body[0, 1]-=m*vertices[i][0]*vertices[i][1];
			I_body[0, 2]-=m*vertices[i][0]*vertices[i][2];
			I_body[1, 0]-=m*vertices[i][1]*vertices[i][0];
			I_body[1, 1]-=m*vertices[i][1]*vertices[i][1];
			I_body[1, 2]-=m*vertices[i][1]*vertices[i][2];
			I_body[2, 0]-=m*vertices[i][2]*vertices[i][0];
			I_body[2, 1]-=m*vertices[i][2]*vertices[i][1];
			I_body[2, 2]-=m*vertices[i][2]*vertices[i][2];
		}
		I_body [3, 3] = 1;
	}
	
	Matrix4x4 Get_Cross_Matrix(Vector3 a)
	{
		//Get the cross product matrix of vector a
		Matrix4x4 A = Matrix4x4.zero;
		A [0, 0] = 0; 
		A [0, 1] = -a[2]; 
		A [0, 2] = a[1]; 
		A [1, 0] = a[2]; 
		A [1, 1] = 0; 
		A [1, 2] = -a[0]; 
		A [2, 0] = -a[1]; 
		A [2, 1] = a[0]; 
		A [2, 2] = 0; 
		A [3, 3] = 1;
		return A;
    }
    Matrix4x4 Rotation_Matrix(Quaternion q)
    {
        Matrix4x4 R = Matrix4x4.zero;
        R[0, 0] = q[3] * q[3] + q[0] * q[0] - q[1] * q[1] - q[2] * q[2];
        R[0, 1] = (q[0] * q[1] - q[2] * q[3])*2;
        R[0, 2] = (q[0] * q[2] + q[1] * q[3])*2;
        R[1, 0] = (q[0] * q[1] + q[2] * q[3])*2;
        R[1, 1] = q[3] * q[3] - q[0] * q[0] + q[1] * q[1] - q[2] * q[2];
        R[1, 2] = (q[1] * q[2] - q[0] * q[3])*2;
        R[2, 0] = (q[0] * q[2] - q[1] * q[3])*2;
        R[2, 1] = (q[1] * q[2] + q[0] * q[3])*2;
        R[2, 2] = q[3] * q[3] - q[0] * q[0] - q[1] * q[1] + q[2] * q[2];
        R[3, 3] = 1;

        return R;

    }

    // Update is called once per frame
    void Update () 
	{
        // Use this as your time step
		float dt = 0.02f;
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        


        if (Input.GetKey(KeyCode.Alpha1))
        {
            launched = true;
        }
        // Part I: Update velocities
        
        Vector3 linearAcc = new Vector3(0, 0.0f, 0);
        if (launched)
        {
            Vector3 force = new Vector3(40.0f, -9.8f * mass,0);
            v = new Vector3(6.5f,2.0f,0);
            linearAcc = force/mass;
            ifHasG = true;         
        }
        if (ifHasG)
        {
            linearAcc = new Vector3(linearAcc.x, -9.8f, linearAcc.z);
        }
        v = v + dt * linearAcc;

        v = v * linear_damping;
        w = w * angular_damping;
        // Part II: Collision Handler

        
        Vector3 ground_N = new Vector3(0, 1.0f, 0);
        Vector3 wall_N = new Vector3(-1.0f, 0, 0);
        Vector3 riSUM = new Vector3(0, 0, 0);
        int c = 0;
        Vector3 rit;
        Vector3 xit;
        Vector3 vit;
        Matrix4x4 RotationM = Rotation_Matrix(q);
        bool ifTouchGround = false;
        bool ifTouchWall = false;
        /*
         * Detect collision
         * xi(t) · N = (x(t) + ri(t)) · N < 0,
         * vi(t) · N = (vt + ω(t) × ri(t)) · N < 0
         */
        foreach (Vector3 vertex in vertices)
        {
            rit = RotationM * vertex;
            xit = new Vector3(transform.position.x, transform.position.y, transform.position.z)+rit;
            vit = v+Vector3.Cross(w,rit);
            Vector3 new_xit= new Vector3(xit.x, xit.y-0.35f, xit.z);
            ifTouchGround = (Vector3.Dot(new_xit, ground_N) < 0) && (Vector3.Dot(vit, ground_N) < 0);

            Vector3 xit_wall = new Vector3(xit.x - 1.6f, xit.y, xit.z);
            ifTouchWall = (Vector3.Dot(xit_wall, wall_N) < 0) && (Vector3.Dot(vit, wall_N) < 0);
            if (ifTouchGround)
            {
                riSUM += rit;
                c++;
            }
            if (ifTouchWall)
            {
                riSUM += rit;
                c++;
            }

        }
        if (c != 0)
        {
            rit = riSUM / c;
            vit=v+ Vector3.Cross(w, rit);
            Matrix4x4 Ri_star = Get_Cross_Matrix(rit);
            Matrix4x4 I = RotationM * I_body * Matrix4x4.Transpose(RotationM);
            Matrix4x4 unit = Matrix4x4.zero;
            unit.m00 = 1.0f / mass;
            unit.m11 = 1.0f / mass;
            unit.m22 = 1.0f / mass;
            unit.m33 = 1.0f / mass;
            Matrix4x4 K = Matrix4x4.zero;
            Matrix4x4 RsIRs = Ri_star * I.inverse * Ri_star;
            for (int n = 0; n < 4; n++)
            {
                for (int m = 0; m < 4; m++)
                {
                    K[n, m] =unit[n,m]- RsIRs[n, m];
                }
            }
            Vector3 j= new Vector3(0,0,0);
            if (Vector3.Dot(vit,ground_N) < 14.0f)
            {
                restitution = 0;
            }

            if (ifTouchGround)
            {
                j= K.inverse * (-vit - restitution * (Vector3.Dot(vit, ground_N)) * ground_N);
            }
            if (ifTouchWall)
            {
                j =K.inverse * (-vit - restitution * (Vector3.Dot(vit, wall_N)) * wall_N);
            }

            v = v + j / mass;
            Vector4 temp_for_w=I.inverse * (Vector3.Cross(rit, j));
            w = w + new Vector3(temp_for_w.x, temp_for_w.y, temp_for_w.z);
        }

        // Part III: Update position & orientation


        //Update linear status
        Vector3 linear_pos_temp = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        linear_pos_temp = linear_pos_temp + dt * v;
        //Update angular status
        Quaternion temp_q;
        Vector3 temp_v_w = new Vector3(w.x, w.y, w.z);
        Vector3 temp_v_q = new Vector3(q.x, q.y, q.z);
        float w_w = 0;
        float q_w = q.w;
        Vector3 temp = w_w * temp_v_q + q_w * temp_v_w + Vector3.Cross(temp_v_w, temp_v_q);
        float temp_w = w_w * q_w - Vector3.Dot(temp_v_w, temp_v_q);
        temp_q = new Quaternion(v.x * 0.5f * dt, v.y * 0.5f * dt, v.z * 0.5f * dt, temp_w * 0.5f * dt);
        
        q = new Quaternion(q.x+temp_q.x,q.y+temp_q.y,q.z+temp_q.z,q.w+temp_q.w);
        float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        q = new Quaternion(q.x/magnitude,q.y/magnitude,q.z/magnitude,q.w/magnitude);
        // Part IV: Assign to the bunny object

        transform.position = linear_pos_temp;
        transform.rotation = q;

        //Reset Launched
        launched = false;

        if (Input.GetKey(KeyCode.R))
        {
            ifHasG = false;
            
            //Reset linear status
            v = new Vector3(0, 0, 0);

            //Reset angular status
            w = new Vector3(0,40.0f,0);
            //Reset position
            transform.position = new Vector3(0,0.6f,0);
            transform.rotation= Quaternion.identity;
        }
        
    }
}
