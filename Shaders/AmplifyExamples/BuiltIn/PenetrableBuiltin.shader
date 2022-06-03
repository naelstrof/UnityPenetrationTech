// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "PenetrationTech/BuiltIn/ProceduralPenetrable"
{
	Properties
	{
		_BaseColorMap("BaseColorMap", 2D) = "white" {}
		_NormalMap("NormalMap", 2D) = "bump" {}
		_MaskMap("MaskMap", 2D) = "black" {}
		_CompressibleDistance("CompressibleDistance", Range( 0 , 1)) = 0.3
		_Smoothness("Smoothness", Range( 0 , 10)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 5.0
		#include "Packages/com.naelstrof.penetrationtech/Shaders/Penetration.cginc"
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float _CompressibleDistance;
		uniform float _Smoothness;
		uniform sampler2D _NormalMap;
		uniform float4 _NormalMap_ST;
		uniform sampler2D _BaseColorMap;
		uniform float4 _BaseColorMap_ST;
		uniform sampler2D _MaskMap;
		uniform float4 _MaskMap_ST;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float localGetDeformationFromPenetrators_float8_g2 = ( 0.0 );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float4 appendResult17_g2 = (float4(ase_vertex3Pos , 1.0));
			float4 transform16_g2 = mul(unity_ObjectToWorld,appendResult17_g2);
			float3 worldPosition8_g2 = (transform16_g2).xyz;
			float4 uv28_g2 = v.texcoord2;
			float compressibleDistance8_g2 = _CompressibleDistance;
			float smoothness8_g2 = _Smoothness;
			float3 deformedPosition8_g2 = float3( 0,0,0 );
			{
			GetDeformationFromPenetrators_float(worldPosition8_g2,uv28_g2,compressibleDistance8_g2,smoothness8_g2,deformedPosition8_g2);
			}
			float4 appendResult21_g2 = (float4(deformedPosition8_g2 , 1.0));
			float4 transform19_g2 = mul(unity_WorldToObject,appendResult21_g2);
			v.vertex.xyz += (transform19_g2).xyz;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_NormalMap = i.uv_texcoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
			o.Normal = UnpackNormal( tex2D( _NormalMap, uv_NormalMap ) );
			float2 uv_BaseColorMap = i.uv_texcoord * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;
			o.Albedo = tex2D( _BaseColorMap, uv_BaseColorMap ).rgb;
			float2 uv_MaskMap = i.uv_texcoord * _MaskMap_ST.xy + _MaskMap_ST.zw;
			float4 tex2DNode33 = tex2D( _MaskMap, uv_MaskMap );
			o.Metallic = tex2DNode33.r;
			o.Smoothness = tex2DNode33.a;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18912
601;201;1772;924;1106.449;634.0966;1.473469;True;False
Node;AmplifyShaderEditor.RangedFloatNode;82;-355.1239,278.81;Inherit;False;Property;_CompressibleDistance;CompressibleDistance;3;0;Create;True;0;0;0;False;0;False;0.3;0.3;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;83;-349.0253,439.3113;Inherit;False;Property;_Smoothness;Smoothness;4;0;Create;True;0;0;0;False;0;False;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;31;-141.4001,-628.6942;Inherit;True;Property;_BaseColorMap;BaseColorMap;0;0;Create;True;0;0;0;False;0;False;-1;None;e604d44ad233cc04885cf4d8d69671c6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;32;-140.4134,-436.8296;Inherit;True;Property;_NormalMap;NormalMap;1;0;Create;True;0;0;0;False;0;False;-1;None;4b6937e068dc59545bb1225b88f63b5f;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;33;-140.4832,-237.4381;Inherit;True;Property;_MaskMap;MaskMap;2;0;Create;True;0;0;0;False;0;False;-1;None;0c0b372920fd1d24ab789377696bf628;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;84;61.94604,247.5217;Inherit;False;PenetrableDeformation;-1;;2;014b2db8766710a4c8429222ab5b0977;0;4;10;FLOAT3;0,0,0;False;11;FLOAT4;0,0,0,0;False;12;FLOAT;0;False;13;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;93;692.908,-13.58231;Float;False;True;-1;7;ASEMaterialInspector;0;0;Standard;PenetrationTech/BuiltIn/ProceduralPenetrable;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;84;12;82;0
WireConnection;84;13;83;0
WireConnection;93;0;31;0
WireConnection;93;1;32;0
WireConnection;93;3;33;1
WireConnection;93;4;33;4
WireConnection;93;11;84;0
ASEEND*/
//CHKSM=2B3A3B64AEF4180FE7A4998916ADA8A46408D98B