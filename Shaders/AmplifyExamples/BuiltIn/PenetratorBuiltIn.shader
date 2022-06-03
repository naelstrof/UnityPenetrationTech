// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "PenetrationTech/BuiltIn/Penetrator"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		[HideInInspector]_DickRootWorld("DickRootWorld", Vector) = (0,0,0,0)
		[HideInInspector]_DickForwardWorld("DickForwardWorld", Vector) = (0,0,0,0)
		[HideInInspector]_DickRightWorld("DickRightWorld", Vector) = (0,0,0,0)
		[HideInInspector]_DickUpWorld("DickUpWorld", Vector) = (0,0,0,0)
		[HideInInspector]_StartClip("_StartClip", Float) = 0
		[HideInInspector]_EndClip("_EndClip", Float) = 0
		[HideInInspector]_SquashStretchCorrection("_SquashStretchCorrection", Float) = 1
		[HideInInspector]_DistanceToHole("_DistanceToHole", Float) = 0
		[HideInInspector]_DickWorldLength("_DickWorldLength", Float) = 1
		_BaseColorMap("BaseColorMap", 2D) = "white" {}
		_NormalMap("NormalMap", 2D) = "bump" {}
		_MaskMap("MaskMap", 2D) = "black" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 5.0
		#include "Packages/com.naelstrof.penetrationtech/Shaders/Penetration.cginc"
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
		};

		uniform float3 _DickRootWorld;
		uniform float3 _DickRightWorld;
		uniform float3 _DickUpWorld;
		uniform float3 _DickForwardWorld;
		uniform float _SquashStretchCorrection;
		uniform float _DistanceToHole;
		uniform float _DickWorldLength;
		uniform sampler2D _NormalMap;
		uniform float4 _NormalMap_ST;
		uniform sampler2D _BaseColorMap;
		uniform float4 _BaseColorMap_ST;
		uniform sampler2D _MaskMap;
		uniform float4 _MaskMap_ST;
		uniform float _StartClip;
		uniform float _EndClip;
		uniform float _Cutoff = 0.5;


		float3x3 ChangeOfBasis9_g4( float3 right, float3 up, float3 forward )
		{
			float3x3 basisTransform = 0;
			    basisTransform[0][0] = right.x;
			    basisTransform[0][1] = right.y;
			    basisTransform[0][2] = right.z;
			    basisTransform[1][0] = up.x;
			    basisTransform[1][1] = up.y;
			    basisTransform[1][2] = up.z;
			    basisTransform[2][0] = forward.x;
			    basisTransform[2][1] = forward.y;
			    basisTransform[2][2] = forward.z;
			return basisTransform;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float localToCatmullRomSpace_float56_g4 = ( 0.0 );
			float3 worldDickRootPos56_g4 = _DickRootWorld;
			float3 right9_g4 = _DickRightWorld;
			float3 up9_g4 = _DickUpWorld;
			float3 forward9_g4 = _DickForwardWorld;
			float3x3 localChangeOfBasis9_g4 = ChangeOfBasis9_g4( right9_g4 , up9_g4 , forward9_g4 );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float4 appendResult67_g4 = (float4(ase_vertex3Pos , 1.0));
			float4 transform66_g4 = mul(unity_ObjectToWorld,appendResult67_g4);
			float3 temp_output_68_0_g4 = (transform66_g4).xyz;
			float3 temp_output_12_0_g4 = mul( localChangeOfBasis9_g4, ( temp_output_68_0_g4 - _DickRootWorld ) );
			float3 break15_g4 = temp_output_12_0_g4;
			float temp_output_18_0_g4 = ( break15_g4.z * _SquashStretchCorrection );
			float3 appendResult26_g4 = (float3(break15_g4.x , break15_g4.y , temp_output_18_0_g4));
			float3 appendResult25_g4 = (float3(( break15_g4.x / _SquashStretchCorrection ) , ( break15_g4.y / _SquashStretchCorrection ) , temp_output_18_0_g4));
			float temp_output_17_0_g4 = ( _DistanceToHole * 0.5 );
			float smoothstepResult23_g4 = smoothstep( 0.0 , temp_output_17_0_g4 , temp_output_18_0_g4);
			float smoothstepResult22_g4 = smoothstep( _DistanceToHole , temp_output_17_0_g4 , temp_output_18_0_g4);
			float3 lerpResult31_g4 = lerp( appendResult26_g4 , appendResult25_g4 , min( smoothstepResult23_g4 , smoothstepResult22_g4 ));
			float3 lerpResult32_g4 = lerp( lerpResult31_g4 , ( temp_output_12_0_g4 + ( ( _DistanceToHole - ( _DickWorldLength * ( _DistanceToHole / ( _SquashStretchCorrection * _DickWorldLength ) ) ) ) * float3(0,0,1) ) ) , step( _DistanceToHole , temp_output_18_0_g4 ));
			float3 newPosition44_g4 = ( _DickRootWorld + mul( transpose( localChangeOfBasis9_g4 ), lerpResult32_g4 ) );
			float3 worldPosition56_g4 = newPosition44_g4;
			float3 worldDickForward56_g4 = _DickForwardWorld;
			float3 worldDickUp56_g4 = _DickUpWorld;
			float3 worldDickRight56_g4 = _DickRightWorld;
			float3 ase_vertexNormal = v.normal.xyz;
			float4 appendResult86_g4 = (float4(ase_vertexNormal , 0.0));
			float3 normalizeResult87_g4 = normalize( (mul( unity_ObjectToWorld, appendResult86_g4 )).xyz );
			float3 worldNormal56_g4 = normalizeResult87_g4;
			float4 ase_vertexTangent = v.tangent;
			float4 break93_g4 = ase_vertexTangent;
			float4 appendResult89_g4 = (float4(break93_g4.x , break93_g4.y , break93_g4.z , 0.0));
			float3 normalizeResult91_g4 = normalize( (mul( unity_ObjectToWorld, appendResult89_g4 )).xyz );
			float4 appendResult94_g4 = (float4(normalizeResult91_g4 , break93_g4.w));
			float4 worldTangent56_g4 = appendResult94_g4;
			float3 worldPositionOUT56_g4 = float3( 0,0,0 );
			float3 worldNormalOUT56_g4 = float3( 0,0,0 );
			float4 worldTangentOUT56_g4 = float4( 0,0,0,0 );
			{
			ToCatmullRomSpace_float(worldDickRootPos56_g4,worldPosition56_g4,worldDickForward56_g4,worldDickUp56_g4,worldDickRight56_g4,worldNormal56_g4,worldTangent56_g4,worldPositionOUT56_g4,worldNormalOUT56_g4,worldTangentOUT56_g4);
			}
			float4 appendResult73_g4 = (float4(worldPositionOUT56_g4 , 1.0));
			float4 transform72_g4 = mul(unity_WorldToObject,appendResult73_g4);
			v.vertex.xyz += (transform72_g4).xyz;
			v.vertex.w = 1;
			float4 appendResult75_g4 = (float4(worldNormalOUT56_g4 , 0.0));
			float3 normalizeResult76_g4 = normalize( (mul( unity_WorldToObject, appendResult75_g4 )).xyz );
			v.normal = normalizeResult76_g4;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_NormalMap = i.uv_texcoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
			o.Normal = UnpackNormal( tex2D( _NormalMap, uv_NormalMap ) );
			float2 uv_BaseColorMap = i.uv_texcoord * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;
			float4 tex2DNode1 = tex2D( _BaseColorMap, uv_BaseColorMap );
			o.Albedo = tex2DNode1.rgb;
			float2 uv_MaskMap = i.uv_texcoord * _MaskMap_ST.xy + _MaskMap_ST.zw;
			float4 tex2DNode4 = tex2D( _MaskMap, uv_MaskMap );
			o.Metallic = tex2DNode4.r;
			o.Smoothness = tex2DNode4.a;
			o.Alpha = 1;
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float4 appendResult67_g4 = (float4(ase_vertex3Pos , 1.0));
			float4 transform66_g4 = mul(unity_ObjectToWorld,appendResult67_g4);
			float3 temp_output_68_0_g4 = (transform66_g4).xyz;
			float dotResult42_g4 = dot( _DickForwardWorld , ( temp_output_68_0_g4 - _DickRootWorld ) );
			clip( ( tex2DNode1.a * ( 1.0 - ( step( _StartClip , dotResult42_g4 ) * step( dotResult42_g4 , _EndClip ) ) ) ) - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18912
601;201;1772;924;1369.003;397.4664;1.295334;True;False
Node;AmplifyShaderEditor.SamplerNode;1;-780.2597,-369.7313;Inherit;True;Property;_BaseColorMap;BaseColorMap;11;0;Create;True;0;0;0;False;0;False;-1;None;ba1c697bb13b883479ca46af735ec2ec;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;2;-686.4448,332.8491;Inherit;False;PenetratorDeformation;1;;4;034c1604581464e459076bc562dc2e05;0;3;64;FLOAT3;0,0,0;False;69;FLOAT3;0,0,0;False;71;FLOAT4;0,0,0,0;False;4;FLOAT3;61;FLOAT3;62;FLOAT4;63;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-328.5131,2.428608;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;4;-779.3427,21.5253;Inherit;True;Property;_MaskMap;MaskMap;13;0;Create;True;0;0;0;False;0;False;-1;None;cfe17d7bf1efb734e83e78502ca74a2a;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;5;-779.273,-177.8663;Inherit;True;Property;_NormalMap;NormalMap;12;0;Create;True;0;0;0;False;0;False;-1;None;28a2cbd622a998a48b7d504222765053;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;7;ASEMaterialInspector;0;0;Standard;PenetrationTech/BuiltIn/Penetrator;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;3;0;1;4
WireConnection;3;1;2;0
WireConnection;0;0;1;0
WireConnection;0;1;5;0
WireConnection;0;3;4;1
WireConnection;0;4;4;4
WireConnection;0;10;3;0
WireConnection;0;11;2;61
WireConnection;0;12;2;62
ASEEND*/
//CHKSM=BB9499715251BB7FEEB40B32D4D6F444FF9B59F6