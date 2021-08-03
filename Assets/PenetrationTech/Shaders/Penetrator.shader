// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "PenetrationTech/Standard/Penetrator"
{
	Properties
	{
		_PenetratorOrigin("PenetratorOrigin", Vector) = (0,0,0,0)
		_PenetratorForward("PenetratorForward", Vector) = (0,0,1,0)
		_PenetratorLength("PenetratorLength", Float) = 1
		_PenetratorUp("PenetratorUp", Vector) = (0,1,0,0)
		_PenetratorRight("PenetratorRight", Vector) = (1,0,0,0)
		_OrifaceOutWorldPosition1("OrifaceOutWorldPosition1", Vector) = (0,0,0,0)
		_OrifaceOutWorldPosition3("OrifaceOutWorldPosition3", Vector) = (0,0,0,0)
		_OrifaceWorldPosition("OrifaceWorldPosition", Vector) = (0,0,0,0)
		_OrifaceOutWorldPosition2("OrifaceOutWorldPosition2", Vector) = (0,0,0,0)
		_OrifaceWorldNormal("OrifaceWorldNormal", Vector) = (0,0,0,0)
		_PenetrationDepth("PenetrationDepth", Range( -1 , 10)) = 0
		_PenetratorBlendshapeMultiplier("PenetratorBlendshapeMultiplier", Range( 0 , 100)) = 1
		_OrifaceLength("OrifaceLength", Float) = 0
		_PenetratorBulgePercentage("PenetratorBulgePercentage", Range( 0 , 1)) = 0
		_PenetratorCumProgress("PenetratorCumProgress", Range( -1 , 2)) = 0
		_PenetratorSquishPullAmount("PenetratorSquishPullAmount", Range( -1 , 1)) = 0
		_PenetratorCumActive("PenetratorCumActive", Range( 0 , 1)) = 0
		[Toggle(_DEFORM_BALLS_ON)] _DEFORM_BALLS("DEFORM_BALLS", Float) = 0
		[Toggle(_NOBLENDSHAPES_ON)] _NOBLENDSHAPES("NOBLENDSHAPES", Float) = 0
		[Toggle(_CLIP_DICK_ON)] _CLIP_DICK("CLIP_DICK", Float) = 0
		[Toggle(_INVISIBLE_WHEN_INSIDE_ON)] _INVISIBLE_WHEN_INSIDE("INVISIBLE_WHEN_INSIDE", Float) = 0
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_MainTex("MainTex", 2D) = "white" {}
		_MaskMap("MaskMap", 2D) = "gray" {}
		_BumpMap("BumpMap", 2D) = "bump" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma shader_feature_local _DEFORM_BALLS_ON
		#pragma multi_compile_local __ _CLIP_DICK_ON
		#pragma multi_compile_local __ _INVISIBLE_WHEN_INSIDE_ON
		#pragma multi_compile_local __ _NOBLENDSHAPES_ON
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
			float vertexToFrag250_g1;
		};

		uniform float _PenetratorBlendshapeMultiplier;
		uniform float _PenetratorLength;
		uniform float _PenetrationDepth;
		uniform float3 _PenetratorOrigin;
		uniform float3 _OrifaceWorldPosition;
		uniform float3 _PenetratorUp;
		uniform float3 _PenetratorForward;
		uniform float _PenetratorBulgePercentage;
		uniform float _PenetratorSquishPullAmount;
		uniform float _PenetratorCumProgress;
		uniform float _PenetratorCumActive;
		uniform float _OrifaceLength;
		uniform float3 _PenetratorRight;
		uniform float3 _OrifaceWorldNormal;
		uniform float3 _OrifaceOutWorldPosition1;
		uniform float3 _OrifaceOutWorldPosition2;
		uniform float3 _OrifaceOutWorldPosition3;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _MaskMap;
		uniform float4 _MaskMap_ST;
		uniform float _Cutoff = 0.5;


		float3 MyCustomExpression20_g964( float3 bezierDerivitive, float3 forward, float3 up )
		{
			float bezierUpness = dot( bezierDerivitive , up);
			float3 bezierUp = lerp( up , -forward , saturate( bezierUpness ));
			float bezierDownness = dot( bezierDerivitive , -up );
			return normalize( lerp( bezierUp , forward , saturate( bezierDownness )) );
		}


		float3 MyCustomExpression20_g952( float3 bezierDerivitive, float3 forward, float3 up )
		{
			float bezierUpness = dot( bezierDerivitive , up);
			float3 bezierUp = lerp( up , -forward , saturate( bezierUpness ));
			float bezierDownness = dot( bezierDerivitive , -up );
			return normalize( lerp( bezierUp , forward , saturate( bezierDownness )) );
		}


		float3 MyCustomExpression20_g957( float3 bezierDerivitive, float3 forward, float3 up )
		{
			float bezierUpness = dot( bezierDerivitive , up);
			float3 bezierUp = lerp( up , -forward , saturate( bezierUpness ));
			float bezierDownness = dot( bezierDerivitive , -up );
			return normalize( lerp( bezierUp , forward , saturate( bezierDownness )) );
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertexNormal = v.normal.xyz;
			float3 VertexNormal259_g1 = ase_vertexNormal;
			float3 normalizeResult27_g962 = normalize( VertexNormal259_g1 );
			float3 temp_output_35_0_g1 = normalizeResult27_g962;
			float4 ase_vertexTangent = v.tangent;
			float3 normalizeResult31_g962 = normalize( ase_vertexTangent.xyz );
			float3 normalizeResult29_g962 = normalize( cross( normalizeResult27_g962 , normalizeResult31_g962 ) );
			float3 temp_output_35_1_g1 = cross( normalizeResult29_g962 , normalizeResult27_g962 );
			float3 temp_output_35_2_g1 = normalizeResult29_g962;
			float3 SquishDelta85_g1 = ( ( ( temp_output_35_0_g1 * v.texcoord2.x ) + ( temp_output_35_1_g1 * v.texcoord2.y ) + ( temp_output_35_2_g1 * v.texcoord2.z ) ) * _PenetratorBlendshapeMultiplier );
			float temp_output_234_0_g1 = length( SquishDelta85_g1 );
			float temp_output_11_0_g1 = max( _PenetrationDepth , 0.0 );
			float VisibleLength25_g1 = ( _PenetratorLength * ( 1.0 - temp_output_11_0_g1 ) );
			float3 DickOrigin16_g1 = _PenetratorOrigin;
			float4 appendResult132_g1 = (float4(_OrifaceWorldPosition , 1.0));
			float4 transform140_g1 = mul(unity_WorldToObject,appendResult132_g1);
			float3 OrifacePosition170_g1 = (transform140_g1).xyz;
			float DickLength19_g1 = _PenetratorLength;
			float3 DickUp172_g1 = _PenetratorUp;
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 VertexPosition254_g1 = ase_vertex3Pos;
			float3 temp_output_27_0_g1 = ( VertexPosition254_g1 - DickOrigin16_g1 );
			float3 DickForward18_g1 = _PenetratorForward;
			float dotResult42_g1 = dot( temp_output_27_0_g1 , DickForward18_g1 );
			float BulgePercentage37_g1 = _PenetratorBulgePercentage;
			float temp_output_1_0_g946 = saturate( ( abs( ( dotResult42_g1 - VisibleLength25_g1 ) ) / ( DickLength19_g1 * BulgePercentage37_g1 ) ) );
			float temp_output_94_0_g1 = sqrt( ( 1.0 - ( temp_output_1_0_g946 * temp_output_1_0_g946 ) ) );
			float3 PullDelta91_g1 = ( ( ( temp_output_35_0_g1 * v.texcoord3.x ) + ( temp_output_35_1_g1 * v.texcoord3.y ) + ( temp_output_35_2_g1 * v.texcoord3.z ) ) * _PenetratorBlendshapeMultiplier );
			float dotResult32_g1 = dot( temp_output_27_0_g1 , DickForward18_g1 );
			float temp_output_1_0_g961 = saturate( ( abs( ( dotResult32_g1 - ( DickLength19_g1 * _PenetratorCumProgress ) ) ) / ( DickLength19_g1 * BulgePercentage37_g1 ) ) );
			float3 CumDelta90_g1 = ( ( ( temp_output_35_0_g1 * v.texcoord1.w ) + ( temp_output_35_1_g1 * v.texcoord2.w ) + ( temp_output_35_2_g1 * v.texcoord3.w ) ) * _PenetratorBlendshapeMultiplier );
			#ifdef _NOBLENDSHAPES_ON
				float3 staticSwitch390_g1 = VertexPosition254_g1;
			#else
				float3 staticSwitch390_g1 = ( VertexPosition254_g1 + ( SquishDelta85_g1 * temp_output_94_0_g1 * saturate( -_PenetratorSquishPullAmount ) ) + ( temp_output_94_0_g1 * PullDelta91_g1 * saturate( _PenetratorSquishPullAmount ) ) + ( sqrt( ( 1.0 - ( temp_output_1_0_g961 * temp_output_1_0_g961 ) ) ) * CumDelta90_g1 * _PenetratorCumActive ) );
			#endif
			float dotResult118_g1 = dot( ( staticSwitch390_g1 - DickOrigin16_g1 ) , DickForward18_g1 );
			float PenetrationDepth39_g1 = _PenetrationDepth;
			float temp_output_65_0_g1 = ( PenetrationDepth39_g1 * DickLength19_g1 );
			float OrifaceLength34_g1 = _OrifaceLength;
			float temp_output_73_0_g1 = ( 0.25 * OrifaceLength34_g1 );
			float dotResult80_g1 = dot( ( staticSwitch390_g1 - DickOrigin16_g1 ) , DickForward18_g1 );
			float temp_output_112_0_g1 = ( -( ( ( temp_output_65_0_g1 - temp_output_73_0_g1 ) + dotResult80_g1 ) - DickLength19_g1 ) * 10.0 );
			#ifdef _CLIP_DICK_ON
				float staticSwitch117_g1 = temp_output_112_0_g1;
			#else
				float staticSwitch117_g1 = max( temp_output_112_0_g1 , ( ( ( temp_output_65_0_g1 + dotResult80_g1 + temp_output_73_0_g1 ) - ( OrifaceLength34_g1 + DickLength19_g1 ) ) * 10.0 ) );
			#endif
			float InsideLerp123_g1 = saturate( staticSwitch117_g1 );
			float3 lerpResult124_g1 = lerp( ( ( DickForward18_g1 * dotResult118_g1 ) + DickOrigin16_g1 ) , staticSwitch390_g1 , InsideLerp123_g1);
			#ifdef _INVISIBLE_WHEN_INSIDE_ON
				float3 staticSwitch125_g1 = lerpResult124_g1;
			#else
				float3 staticSwitch125_g1 = staticSwitch390_g1;
			#endif
			float3 temp_output_354_0_g1 = ( staticSwitch125_g1 - DickOrigin16_g1 );
			float dotResult373_g1 = dot( DickUp172_g1 , temp_output_354_0_g1 );
			float3 DickRight184_g1 = _PenetratorRight;
			float dotResult374_g1 = dot( DickRight184_g1 , temp_output_354_0_g1 );
			float dotResult375_g1 = dot( temp_output_354_0_g1 , DickForward18_g1 );
			float3 lerpResult343_g1 = lerp( ( ( ( saturate( ( ( VisibleLength25_g1 - distance( DickOrigin16_g1 , OrifacePosition170_g1 ) ) / DickLength19_g1 ) ) + 1.0 ) * dotResult373_g1 * DickUp172_g1 ) + ( ( saturate( ( ( VisibleLength25_g1 - distance( DickOrigin16_g1 , OrifacePosition170_g1 ) ) / DickLength19_g1 ) ) + 1.0 ) * dotResult374_g1 * DickRight184_g1 ) + ( DickForward18_g1 * dotResult375_g1 ) + DickOrigin16_g1 ) , staticSwitch125_g1 , saturate( PenetrationDepth39_g1 ));
			float3 originalPosition126_g1 = lerpResult343_g1;
			float dotResult177_g1 = dot( ( originalPosition126_g1 - DickOrigin16_g1 ) , DickForward18_g1 );
			float temp_output_178_0_g1 = max( VisibleLength25_g1 , 0.05 );
			float temp_output_42_0_g963 = ( dotResult177_g1 / temp_output_178_0_g1 );
			float temp_output_26_0_g967 = temp_output_42_0_g963;
			float temp_output_19_0_g967 = ( 1.0 - temp_output_26_0_g967 );
			float4 appendResult130_g1 = (float4(_OrifaceWorldNormal , 0.0));
			float4 transform135_g1 = mul(unity_WorldToObject,appendResult130_g1);
			float3 OrifaceNormal155_g1 = (transform135_g1).xyz;
			float temp_output_396_0_g1 = min( temp_output_178_0_g1 , distance( DickOrigin16_g1 , OrifacePosition170_g1 ) );
			float dotResult398_g1 = dot( ( DickOrigin16_g1 - OrifacePosition170_g1 ) , OrifaceNormal155_g1 );
			float3 lerpResult401_g1 = lerp( ( OrifacePosition170_g1 + ( OrifaceNormal155_g1 * temp_output_396_0_g1 * 0.5 ) ) , DickOrigin16_g1 , saturate( sign( dotResult398_g1 ) ));
			float3 temp_output_8_0_g963 = lerpResult401_g1;
			float3 temp_output_9_0_g963 = ( lerpResult401_g1 + ( DickForward18_g1 * temp_output_396_0_g1 * 0.25 ) );
			float3 temp_output_10_0_g963 = ( OrifacePosition170_g1 + ( OrifaceNormal155_g1 * 0.25 * temp_output_396_0_g1 ) );
			float3 temp_output_11_0_g963 = OrifacePosition170_g1;
			float temp_output_1_0_g965 = temp_output_42_0_g963;
			float temp_output_8_0_g965 = ( 1.0 - temp_output_1_0_g965 );
			float3 temp_output_3_0_g965 = temp_output_9_0_g963;
			float3 temp_output_4_0_g965 = temp_output_10_0_g963;
			float3 temp_output_7_0_g964 = ( ( 3.0 * temp_output_8_0_g965 * temp_output_8_0_g965 * ( temp_output_3_0_g965 - temp_output_8_0_g963 ) ) + ( 6.0 * temp_output_8_0_g965 * temp_output_1_0_g965 * ( temp_output_4_0_g965 - temp_output_3_0_g965 ) ) + ( 3.0 * temp_output_1_0_g965 * temp_output_1_0_g965 * ( temp_output_11_0_g963 - temp_output_4_0_g965 ) ) );
			float3 normalizeResult27_g966 = normalize( temp_output_7_0_g964 );
			float3 bezierDerivitive20_g964 = temp_output_7_0_g964;
			float3 temp_output_3_0_g963 = DickForward18_g1;
			float3 forward20_g964 = temp_output_3_0_g963;
			float3 temp_output_4_0_g963 = DickUp172_g1;
			float3 up20_g964 = temp_output_4_0_g963;
			float3 localMyCustomExpression20_g964 = MyCustomExpression20_g964( bezierDerivitive20_g964 , forward20_g964 , up20_g964 );
			float3 normalizeResult31_g966 = normalize( localMyCustomExpression20_g964 );
			float3 normalizeResult29_g966 = normalize( cross( normalizeResult27_g966 , normalizeResult31_g966 ) );
			float3 temp_output_65_22_g963 = normalizeResult29_g966;
			float3 temp_output_2_0_g963 = ( originalPosition126_g1 - DickOrigin16_g1 );
			float3 temp_output_5_0_g963 = DickRight184_g1;
			float dotResult15_g963 = dot( temp_output_2_0_g963 , temp_output_5_0_g963 );
			float3 temp_output_65_0_g963 = cross( normalizeResult29_g966 , normalizeResult27_g966 );
			float dotResult18_g963 = dot( temp_output_2_0_g963 , temp_output_4_0_g963 );
			float dotResult142_g1 = dot( ( originalPosition126_g1 - DickOrigin16_g1 ) , DickForward18_g1 );
			float temp_output_152_0_g1 = ( dotResult142_g1 - VisibleLength25_g1 );
			float temp_output_157_0_g1 = ( temp_output_152_0_g1 / OrifaceLength34_g1 );
			#ifdef _CLIP_DICK_ON
				float staticSwitch197_g1 = min( temp_output_157_0_g1 , 1.0 );
			#else
				float staticSwitch197_g1 = temp_output_157_0_g1;
			#endif
			float temp_output_42_0_g951 = staticSwitch197_g1;
			float temp_output_26_0_g955 = temp_output_42_0_g951;
			float temp_output_19_0_g955 = ( 1.0 - temp_output_26_0_g955 );
			float3 temp_output_8_0_g951 = OrifacePosition170_g1;
			float4 appendResult145_g1 = (float4(_OrifaceOutWorldPosition1 , 1.0));
			float4 transform151_g1 = mul(unity_WorldToObject,appendResult145_g1);
			float3 OrifaceOutPosition1183_g1 = (transform151_g1).xyz;
			float3 temp_output_9_0_g951 = OrifaceOutPosition1183_g1;
			float4 appendResult144_g1 = (float4(_OrifaceOutWorldPosition2 , 1.0));
			float4 transform154_g1 = mul(unity_WorldToObject,appendResult144_g1);
			float3 OrifaceOutPosition2182_g1 = (transform154_g1).xyz;
			float3 temp_output_10_0_g951 = OrifaceOutPosition2182_g1;
			float4 appendResult143_g1 = (float4(_OrifaceOutWorldPosition3 , 1.0));
			float4 transform147_g1 = mul(unity_WorldToObject,appendResult143_g1);
			float3 OrifaceOutPosition3175_g1 = (transform147_g1).xyz;
			float3 temp_output_11_0_g951 = OrifaceOutPosition3175_g1;
			float temp_output_1_0_g953 = temp_output_42_0_g951;
			float temp_output_8_0_g953 = ( 1.0 - temp_output_1_0_g953 );
			float3 temp_output_3_0_g953 = temp_output_9_0_g951;
			float3 temp_output_4_0_g953 = temp_output_10_0_g951;
			float3 temp_output_7_0_g952 = ( ( 3.0 * temp_output_8_0_g953 * temp_output_8_0_g953 * ( temp_output_3_0_g953 - temp_output_8_0_g951 ) ) + ( 6.0 * temp_output_8_0_g953 * temp_output_1_0_g953 * ( temp_output_4_0_g953 - temp_output_3_0_g953 ) ) + ( 3.0 * temp_output_1_0_g953 * temp_output_1_0_g953 * ( temp_output_11_0_g951 - temp_output_4_0_g953 ) ) );
			float3 normalizeResult27_g954 = normalize( temp_output_7_0_g952 );
			float3 bezierDerivitive20_g952 = temp_output_7_0_g952;
			float3 temp_output_3_0_g951 = DickForward18_g1;
			float3 forward20_g952 = temp_output_3_0_g951;
			float3 temp_output_4_0_g951 = DickUp172_g1;
			float3 up20_g952 = temp_output_4_0_g951;
			float3 localMyCustomExpression20_g952 = MyCustomExpression20_g952( bezierDerivitive20_g952 , forward20_g952 , up20_g952 );
			float3 normalizeResult31_g954 = normalize( localMyCustomExpression20_g952 );
			float3 normalizeResult29_g954 = normalize( cross( normalizeResult27_g954 , normalizeResult31_g954 ) );
			float3 temp_output_65_22_g951 = normalizeResult29_g954;
			float3 temp_output_2_0_g951 = ( originalPosition126_g1 - DickOrigin16_g1 );
			float3 temp_output_5_0_g951 = DickRight184_g1;
			float dotResult15_g951 = dot( temp_output_2_0_g951 , temp_output_5_0_g951 );
			float3 temp_output_65_0_g951 = cross( normalizeResult29_g954 , normalizeResult27_g954 );
			float dotResult18_g951 = dot( temp_output_2_0_g951 , temp_output_4_0_g951 );
			float temp_output_208_0_g1 = saturate( sign( temp_output_152_0_g1 ) );
			float3 lerpResult221_g1 = lerp( ( ( ( temp_output_19_0_g967 * temp_output_19_0_g967 * temp_output_19_0_g967 * temp_output_8_0_g963 ) + ( temp_output_19_0_g967 * temp_output_19_0_g967 * 3.0 * temp_output_26_0_g967 * temp_output_9_0_g963 ) + ( 3.0 * temp_output_19_0_g967 * temp_output_26_0_g967 * temp_output_26_0_g967 * temp_output_10_0_g963 ) + ( temp_output_26_0_g967 * temp_output_26_0_g967 * temp_output_26_0_g967 * temp_output_11_0_g963 ) ) + ( temp_output_65_22_g963 * dotResult15_g963 ) + ( temp_output_65_0_g963 * dotResult18_g963 ) ) , ( ( ( temp_output_19_0_g955 * temp_output_19_0_g955 * temp_output_19_0_g955 * temp_output_8_0_g951 ) + ( temp_output_19_0_g955 * temp_output_19_0_g955 * 3.0 * temp_output_26_0_g955 * temp_output_9_0_g951 ) + ( 3.0 * temp_output_19_0_g955 * temp_output_26_0_g955 * temp_output_26_0_g955 * temp_output_10_0_g951 ) + ( temp_output_26_0_g955 * temp_output_26_0_g955 * temp_output_26_0_g955 * temp_output_11_0_g951 ) ) + ( temp_output_65_22_g951 * dotResult15_g951 ) + ( temp_output_65_0_g951 * dotResult18_g951 ) ) , temp_output_208_0_g1);
			float3 temp_output_42_0_g956 = DickForward18_g1;
			float NonVisibleLength165_g1 = ( temp_output_11_0_g1 * _PenetratorLength );
			float3 temp_output_52_0_g956 = ( ( temp_output_42_0_g956 * ( ( NonVisibleLength165_g1 - OrifaceLength34_g1 ) - DickLength19_g1 ) ) + ( originalPosition126_g1 - DickOrigin16_g1 ) );
			float dotResult53_g956 = dot( temp_output_42_0_g956 , temp_output_52_0_g956 );
			float temp_output_1_0_g958 = 1.0;
			float temp_output_8_0_g958 = ( 1.0 - temp_output_1_0_g958 );
			float3 temp_output_3_0_g958 = OrifaceOutPosition1183_g1;
			float3 temp_output_4_0_g958 = OrifaceOutPosition2182_g1;
			float3 temp_output_7_0_g957 = ( ( 3.0 * temp_output_8_0_g958 * temp_output_8_0_g958 * ( temp_output_3_0_g958 - OrifacePosition170_g1 ) ) + ( 6.0 * temp_output_8_0_g958 * temp_output_1_0_g958 * ( temp_output_4_0_g958 - temp_output_3_0_g958 ) ) + ( 3.0 * temp_output_1_0_g958 * temp_output_1_0_g958 * ( OrifaceOutPosition3175_g1 - temp_output_4_0_g958 ) ) );
			float3 normalizeResult27_g959 = normalize( temp_output_7_0_g957 );
			float3 temp_output_85_23_g956 = normalizeResult27_g959;
			float3 temp_output_4_0_g956 = DickUp172_g1;
			float dotResult54_g956 = dot( temp_output_4_0_g956 , temp_output_52_0_g956 );
			float3 bezierDerivitive20_g957 = temp_output_7_0_g957;
			float3 forward20_g957 = temp_output_42_0_g956;
			float3 up20_g957 = temp_output_4_0_g956;
			float3 localMyCustomExpression20_g957 = MyCustomExpression20_g957( bezierDerivitive20_g957 , forward20_g957 , up20_g957 );
			float3 normalizeResult31_g959 = normalize( localMyCustomExpression20_g957 );
			float3 normalizeResult29_g959 = normalize( cross( normalizeResult27_g959 , normalizeResult31_g959 ) );
			float3 temp_output_85_0_g956 = cross( normalizeResult29_g959 , normalizeResult27_g959 );
			float3 temp_output_43_0_g956 = DickRight184_g1;
			float dotResult55_g956 = dot( temp_output_43_0_g956 , temp_output_52_0_g956 );
			float3 temp_output_85_22_g956 = normalizeResult29_g959;
			float temp_output_222_0_g1 = saturate( sign( ( temp_output_157_0_g1 - 1.0 ) ) );
			float3 lerpResult224_g1 = lerp( lerpResult221_g1 , ( ( ( dotResult53_g956 * temp_output_85_23_g956 ) + ( dotResult54_g956 * temp_output_85_0_g956 ) + ( dotResult55_g956 * temp_output_85_22_g956 ) ) + OrifaceOutPosition3175_g1 ) , temp_output_222_0_g1);
			#ifdef _CLIP_DICK_ON
				float3 staticSwitch229_g1 = lerpResult221_g1;
			#else
				float3 staticSwitch229_g1 = lerpResult224_g1;
			#endif
			float temp_output_226_0_g1 = saturate( -PenetrationDepth39_g1 );
			float3 lerpResult232_g1 = lerp( staticSwitch229_g1 , originalPosition126_g1 , temp_output_226_0_g1);
			float3 ifLocalVar237_g1 = 0;
			if( temp_output_234_0_g1 <= 0.0 )
				ifLocalVar237_g1 = originalPosition126_g1;
			else
				ifLocalVar237_g1 = lerpResult232_g1;
			#ifdef _DEFORM_BALLS_ON
				float3 staticSwitch239_g1 = lerpResult232_g1;
			#else
				float3 staticSwitch239_g1 = ifLocalVar237_g1;
			#endif
			v.vertex.xyz = staticSwitch239_g1;
			v.vertex.w = 1;
			float3 temp_output_21_0_g963 = VertexNormal259_g1;
			float dotResult55_g963 = dot( temp_output_21_0_g963 , temp_output_3_0_g963 );
			float dotResult56_g963 = dot( temp_output_21_0_g963 , temp_output_4_0_g963 );
			float dotResult57_g963 = dot( temp_output_21_0_g963 , temp_output_5_0_g963 );
			float3 normalizeResult31_g963 = normalize( ( ( dotResult55_g963 * normalizeResult27_g966 ) + ( dotResult56_g963 * temp_output_65_0_g963 ) + ( dotResult57_g963 * temp_output_65_22_g963 ) ) );
			float3 temp_output_21_0_g951 = VertexNormal259_g1;
			float dotResult55_g951 = dot( temp_output_21_0_g951 , temp_output_3_0_g951 );
			float dotResult56_g951 = dot( temp_output_21_0_g951 , temp_output_4_0_g951 );
			float dotResult57_g951 = dot( temp_output_21_0_g951 , temp_output_5_0_g951 );
			float3 normalizeResult31_g951 = normalize( ( ( dotResult55_g951 * normalizeResult27_g954 ) + ( dotResult56_g951 * temp_output_65_0_g951 ) + ( dotResult57_g951 * temp_output_65_22_g951 ) ) );
			float3 lerpResult227_g1 = lerp( normalizeResult31_g963 , normalizeResult31_g951 , temp_output_208_0_g1);
			float3 temp_output_24_0_g956 = VertexNormal259_g1;
			float dotResult61_g956 = dot( temp_output_42_0_g956 , temp_output_24_0_g956 );
			float dotResult62_g956 = dot( temp_output_4_0_g956 , temp_output_24_0_g956 );
			float dotResult60_g956 = dot( temp_output_43_0_g956 , temp_output_24_0_g956 );
			float3 normalizeResult33_g956 = normalize( ( ( dotResult61_g956 * temp_output_85_23_g956 ) + ( dotResult62_g956 * temp_output_85_0_g956 ) + ( dotResult60_g956 * temp_output_85_22_g956 ) ) );
			float3 lerpResult233_g1 = lerp( lerpResult227_g1 , normalizeResult33_g956 , temp_output_222_0_g1);
			#ifdef _CLIP_DICK_ON
				float3 staticSwitch236_g1 = lerpResult227_g1;
			#else
				float3 staticSwitch236_g1 = lerpResult233_g1;
			#endif
			float3 lerpResult238_g1 = lerp( staticSwitch236_g1 , VertexNormal259_g1 , temp_output_226_0_g1);
			float3 ifLocalVar391_g1 = 0;
			if( temp_output_234_0_g1 <= 0.0 )
				ifLocalVar391_g1 = VertexNormal259_g1;
			else
				ifLocalVar391_g1 = lerpResult238_g1;
			v.normal = ifLocalVar391_g1;
			#ifdef _INVISIBLE_WHEN_INSIDE_ON
				float staticSwitch249_g1 = InsideLerp123_g1;
			#else
				float staticSwitch249_g1 = 1.0;
			#endif
			o.vertexToFrag250_g1 = staticSwitch249_g1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_BumpMap = i.uv_texcoord * _BumpMap_ST.xy + _BumpMap_ST.zw;
			o.Normal = UnpackNormal( tex2D( _BumpMap, uv_BumpMap ) );
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			o.Albedo = tex2D( _MainTex, uv_MainTex ).rgb;
			float2 uv_MaskMap = i.uv_texcoord * _MaskMap_ST.xy + _MaskMap_ST.zw;
			float4 tex2DNode102 = tex2D( _MaskMap, uv_MaskMap );
			o.Metallic = tex2DNode102.r;
			o.Smoothness = tex2DNode102.a;
			o.Alpha = 1;
			clip( i.vertexToFrag250_g1 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18912
199;363;1675;705;-5908.111;2039.059;2.675143;True;False
Node;AmplifyShaderEditor.SamplerNode;100;7108.375,-1682.497;Inherit;True;Property;_MainTex;MainTex;23;0;Create;True;0;0;0;False;0;False;-1;None;a7fd0f05ef2dbbd43ac64dff86a86708;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;102;7105.585,-1265.636;Inherit;True;Property;_MaskMap;MaskMap;24;0;Create;True;0;0;0;False;0;False;-1;None;b6ba1a4a299c969449e3ec084c188366;True;0;False;gray;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;101;7104.496,-1467.525;Inherit;True;Property;_BumpMap;BumpMap;25;0;Create;True;0;0;0;False;0;False;-1;None;22df23dcaaa4c974888f14b0f36d484f;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;675;7008.464,-1016.773;Inherit;False;PenetrationTechDeformation;0;;1;cb4db099da64a8846a0c6877ff8e2b5f;0;3;253;FLOAT3;0,0,0;False;258;FLOAT3;0,0,0;False;265;FLOAT3;0,0,0;False;3;FLOAT3;0;FLOAT;251;FLOAT3;252
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;552;7623.605,-1459.329;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;PenetrationTech/Standard/Penetrator;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;TransparentCutout;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Absolute;0;;22;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;552;0;100;0
WireConnection;552;1;101;0
WireConnection;552;3;102;1
WireConnection;552;4;102;4
WireConnection;552;10;675;251
WireConnection;552;11;675;0
WireConnection;552;12;675;252
ASEEND*/
//CHKSM=B8B5132F5EF925263A57FB6A57E1A39A945C4E0E