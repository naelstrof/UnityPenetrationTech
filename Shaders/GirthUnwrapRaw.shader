// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "PenetrationTech/GirthUnwrapRaw"
{
	Properties
	{
		_DickForward("DickForward", Vector) = (0,1,0,0)
		_DickOrigin("DickOrigin", Vector) = (0,0,0,0)
		_DickRight("DickRight", Vector) = (1,0,0,0)
		_DickUp("DickUp", Vector) = (0,0,1,0)
		_MaxLength("MaxLength", Float) = 1
		_MaxGirth("MaxGirth", Float) = 0.25
		_Blend("Blend", Range( 0 , 1)) = 1
		_AngleOffset("AngleOffset", Range( -3.141593 , 3.141593)) = 0

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Opaque" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha
		BlendOp Max
		AlphaToMask Off
		Cull Front
		ColorMask RGBA
		ZWrite Off
		ZTest Always
		Offset 0 , 0
		
		
		
		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_FRAG_POSITION


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
				#endif
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform float3 _DickOrigin;
			uniform float3 _DickForward;
			uniform float _MaxLength;
			uniform float _AngleOffset;
			uniform float3 _DickUp;
			uniform float3 _DickRight;
			uniform float _Blend;
			uniform float _MaxGirth;
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			
			float MyCustomExpression82( float pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				return pos;
				#else
				return pos-6.28318530718;
				#endif
			}
			
			float3 MyCustomExpression81( float3 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				return pos;
				#else
				return float3(pos.x,1-pos.y,0);
				#endif
			}
			

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float3 temp_output_21_0 = ( v.vertex.xyz - _DickOrigin );
				float dotResult22 = dot( temp_output_21_0 , _DickForward );
				float3 rotatedValue65 = RotateAroundAxis( float3( 0,0,0 ), temp_output_21_0, _DickForward, _AngleOffset );
				float dotResult11 = dot( rotatedValue65 , _DickUp );
				float dotResult13 = dot( rotatedValue65 , _DickRight );
				float temp_output_14_0 = atan2( dotResult11 , dotResult13 );
				float pos82 = _AngleOffset;
				float localMyCustomExpression82 = MyCustomExpression82( pos82 );
				float3 appendResult19 = (float3(( dotResult22 / _MaxLength ) , ( ( temp_output_14_0 - localMyCustomExpression82 ) / ( 2.0 * UNITY_PI ) ) , 0.0));
				float3 pos81 = appendResult19;
				float3 localMyCustomExpression81 = MyCustomExpression81( pos81 );
				float4 appendResult59 = (float4(( ( localMyCustomExpression81 - float3( 0.5,0,0 ) ) * float3( 2,2,0 ) ) , 1.0));
				
				o.ase_texcoord1 = v.vertex;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				float4 vertexOverride = appendResult59;
				float vertexOverrideBlend = _Blend;
				o.vertex = lerp(UnityObjectToClipPos(v.vertex), vertexOverride, vertexOverrideBlend);

				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = lerp(mul(unity_ObjectToWorld, v.vertex).xyz, mul(UNITY_MATRIX_I_V, v.vertex).xyz, vertexOverrideBlend);
				#endif
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				float3 temp_output_21_0 = ( i.ase_texcoord1.xyz - _DickOrigin );
				float dotResult22 = dot( temp_output_21_0 , _DickForward );
				float3 rotatedValue65 = RotateAroundAxis( float3( 0,0,0 ), temp_output_21_0, _DickForward, _AngleOffset );
				float dotResult11 = dot( rotatedValue65 , _DickUp );
				float dotResult13 = dot( rotatedValue65 , _DickRight );
				float temp_output_14_0 = atan2( dotResult11 , dotResult13 );
				float lerpResult74 = lerp( 0.0 , ( distance( i.ase_texcoord1.xyz , ( _DickOrigin + ( _DickForward * dotResult22 ) ) ) / _MaxGirth ) , ( step( -2.91 , temp_output_14_0 ) * step( temp_output_14_0 , 2.91 ) ));
				float4 appendResult28 = (float4(lerpResult74 , 0.0 , 0.0 , 1.0));
				
				
				finalColor = appendResult28;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=18912
284;483;1800;930;2291.304;1375.692;1.6;True;False
Node;AmplifyShaderEditor.Vector3Node;8;-2577.845,-700.1675;Inherit;False;Property;_DickOrigin;DickOrigin;1;0;Create;True;0;0;0;False;0;False;0,0,0;-6.903836E-07,0,6.770761E-07;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PosVertexDataNode;12;-2595.315,-888.9568;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;63;-1447.726,-1283.6;Inherit;False;Property;_AngleOffset;AngleOffset;7;0;Create;True;0;0;0;False;0;False;0;0;-3.141593;3.141593;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;7;-2605.619,-488.7494;Inherit;False;Property;_DickForward;DickForward;0;0;Create;True;0;0;0;False;0;False;0,1,0;2.083291E-07,1,3.626013E-07;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;21;-1497.295,-690.9421;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;80;-2605.568,-112.2128;Inherit;False;Property;_DickUp;DickUp;3;0;Create;True;0;0;0;False;0;False;0,0,1;1,-8.648601E-09,-8.334374E-08;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RotateAboutAxisNode;65;-1171.311,-712.9716;Inherit;False;False;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;9;-2618.66,-285.201;Inherit;False;Property;_DickRight;DickRight;2;0;Create;True;0;0;0;False;0;False;1,0,0;1,-8.648601E-09,-8.334374E-08;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DotProductOpNode;13;-590.1182,-234.5801;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;11;-664,-511.5;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;82;-785.6527,-1200.987;Inherit;False;#if UNITY_UV_STARTS_AT_TOP$return pos@$#else$return pos-6.28318530718@$#endif;1;Create;1;True;pos;FLOAT;0;In;;Inherit;False;My Custom Expression;True;False;0;;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ATan2OpNode;14;-471.1455,-362.436;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;18;-454.9471,-190.4255;Inherit;False;1;0;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;22;-1263.215,-69.00883;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-752.5352,214.3932;Inherit;False;Property;_MaxLength;MaxLength;4;0;Create;True;0;0;0;False;0;False;1;0.443;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;79;-343.8708,-417.0852;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-962.8484,-133.5928;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;24;-379.5339,-46.67804;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;17;-207.4471,-297.1255;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;19;-50.10468,-304.6881;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;-785.7855,-289.0513;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CustomExpressionNode;81;61.58301,-160.2228;Inherit;False;#if UNITY_UV_STARTS_AT_TOP$return pos@$#else$return float3(pos.x,1-pos.y,0)@$#endif;3;Create;1;True;pos;FLOAT3;0,0,0;In;;Inherit;False;My Custom Expression;True;False;0;;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DistanceOpNode;25;-565.8964,-797.9518;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;73;-170.0632,-672.6143;Inherit;False;2;0;FLOAT;-2.91;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;75;-169.01,-559.3608;Inherit;False;2;0;FLOAT;0.05;False;1;FLOAT;2.91;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;60;-500.6877,-641.472;Inherit;False;Property;_MaxGirth;MaxGirth;5;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;78;-12.01001,-660.3608;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;48;166.1102,-336.2197;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;62;-312.5122,-798.739;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;330.3685,-309.8466;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;2,2,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;74;40.33679,-905.3147;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;28;235.3312,-803.8737;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;59;514.9286,-318.5195;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;47;198.5742,40.46842;Inherit;False;Property;_Blend;Blend;6;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;61;747.3164,-442.2791;Float;False;True;-1;2;ASEMaterialInspector;100;17;PenetrationTech/GirthUnwrapRaw;de9302d9d05e26849a28e8f751f34ede;True;Unlit;0;0;Unlit;4;True;True;2;5;False;47;10;False;-1;0;1;False;-1;0;False;-1;True;5;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;True;0;False;-1;True;True;1;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;True;2;False;-1;True;7;False;-1;True;True;0;False;-1;0;False;-1;True;1;RenderType=Opaque=RenderType;True;2;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;;False;0
WireConnection;21;0;12;0
WireConnection;21;1;8;0
WireConnection;65;0;7;0
WireConnection;65;1;63;0
WireConnection;65;3;21;0
WireConnection;13;0;65;0
WireConnection;13;1;9;0
WireConnection;11;0;65;0
WireConnection;11;1;80;0
WireConnection;82;0;63;0
WireConnection;14;0;11;0
WireConnection;14;1;13;0
WireConnection;22;0;21;0
WireConnection;22;1;7;0
WireConnection;79;0;14;0
WireConnection;79;1;82;0
WireConnection;27;0;7;0
WireConnection;27;1;22;0
WireConnection;24;0;22;0
WireConnection;24;1;23;0
WireConnection;17;0;79;0
WireConnection;17;1;18;0
WireConnection;19;0;24;0
WireConnection;19;1;17;0
WireConnection;26;0;8;0
WireConnection;26;1;27;0
WireConnection;81;0;19;0
WireConnection;25;0;12;0
WireConnection;25;1;26;0
WireConnection;73;1;14;0
WireConnection;75;0;14;0
WireConnection;78;0;73;0
WireConnection;78;1;75;0
WireConnection;48;0;81;0
WireConnection;62;0;25;0
WireConnection;62;1;60;0
WireConnection;49;0;48;0
WireConnection;74;1;62;0
WireConnection;74;2;78;0
WireConnection;28;0;74;0
WireConnection;59;0;49;0
WireConnection;61;0;28;0
WireConnection;61;2;59;0
WireConnection;61;3;47;0
ASEEND*/
//CHKSM=62FC94B61093A12361A2F1DBE10F8DF78150B7C4