// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Penetrator"
{
	Properties
	{
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#include "Assets/PenetrationTech/Shaders/ToCatmullRomSpace.cginc"
		#include "Assets/PenetrationTech/Shaders/GetCurveSegment.cginc"
		#include "Assets/PenetrationTech/Shaders/Penetration.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			half filler;
		};

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float distance4 = ase_vertex3Pos.z;
			float localDistanceLUT4 = DistanceLUT( distance4 );
			float t7 = localDistanceLUT4;
			int curveSegmentIndex7 = 0;
			float localGetCurveSegment7 = GetCurveSegment( t7 , curveSegmentIndex7 );
			float3 appendResult6 = (float3(ase_vertex3Pos.x , ase_vertex3Pos.y , localGetCurveSegment7));
			float3 position10 = appendResult6;
			int curveSegmentIndex10 = curveSegmentIndex7;
			float3 localToCatmullRomSpace10 = ToCatmullRomSpace( position10 , curveSegmentIndex10 );
			float4 transform11 = mul(unity_WorldToObject,float4( localToCatmullRomSpace10 , 0.0 ));
			v.vertex.xyz = transform11.xyz;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18912
587;309;1712;979;1799.038;-10.96658;1.3;True;True
Node;AmplifyShaderEditor.PosVertexDataNode;5;-1379.904,264.5366;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CustomExpressionNode;4;-1162.135,505.628;Inherit;False;$;1;File;1;True;distance;FLOAT;0;In;;Inherit;False;DistanceLUT;False;False;0;298838215dc27c84ab5f0abecb052441;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;7;-934.5015,509.9972;Inherit;False; ;1;File;2;True;t;FLOAT;0;In;;Inherit;False;True;curveSegmentIndex;INT;0;Out;;Inherit;False;GetCurveSegment;False;False;0;61a4ea2910cdaf344b507677bfe18270;False;2;0;FLOAT;0;False;1;INT;0;False;2;FLOAT;0;INT;2
Node;AmplifyShaderEditor.DynamicAppendNode;6;-656.3589,316.6466;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CustomExpressionNode;10;-412.5681,398.6884;Inherit;False; ;3;File;2;True;position;FLOAT3;0,0,0;In;;Inherit;False;True;curveSegmentIndex;INT;0;In;;Inherit;False;ToCatmullRomSpace;False;False;0;50d80c5284280404cb4ce0950b6dcec1;False;2;0;FLOAT3;0,0,0;False;1;INT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldToObjectTransfNode;11;-71.32373,417.5734;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;178,152;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Penetrator;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Absolute;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;4;0;5;3
WireConnection;7;0;4;0
WireConnection;6;0;5;1
WireConnection;6;1;5;2
WireConnection;6;2;7;0
WireConnection;10;0;6;0
WireConnection;10;1;7;2
WireConnection;11;0;10;0
WireConnection;0;11;11;0
ASEEND*/
//CHKSM=9378CFE74B35D6C83951E24291D7AF471C24BD08