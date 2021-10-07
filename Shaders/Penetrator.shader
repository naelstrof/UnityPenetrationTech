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
		_OrificeOutWorldPosition1("OrificeOutWorldPosition1", Vector) = (0,0.33,0,0)
		_OrificeOutWorldPosition3("OrificeOutWorldPosition3", Vector) = (0,1,0,0)
		_OrificeWorldPosition("OrificeWorldPosition", Vector) = (0,0,0,0)
		_OrificeOutWorldPosition2("OrificeOutWorldPosition2", Vector) = (0,0.66,0,0)
		_OrificeWorldNormal("OrificeWorldNormal", Vector) = (0,-1,0,0)
		_PenetrationDepth("PenetrationDepth", Range( -1 , 10)) = 0
		_PenetratorBlendshapeMultiplier("PenetratorBlendshapeMultiplier", Range( 0 , 100)) = 1
		_OrificeLength("OrificeLength", Float) = 1
		_PenetratorBulgePercentage("PenetratorBulgePercentage", Range( 0 , 1)) = 0
		_PenetratorCumProgress("PenetratorCumProgress", Range( -1 , 2)) = 0
		_PenetratorSquishPullAmount("PenetratorSquishPullAmount", Range( -1 , 1)) = 0
		_PenetratorCumActive("PenetratorCumActive", Range( 0 , 1)) = 0
		_InvisibleWhenInside("InvisibleWhenInside", Range( 0 , 1)) = 0
		_DeformBalls("DeformBalls", Range( 0 , 1)) = 0
		_ClipDick("ClipDick", Range( 0 , 1)) = 0
		_NoBlendshapes("NoBlendshapes", Range( 0 , 1)) = 0
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
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
			float vertexToFrag250_g979;
		};

		uniform float _PenetratorBlendshapeMultiplier;
		uniform float _PenetratorLength;
		uniform float _PenetrationDepth;
		uniform float3 _PenetratorOrigin;
		uniform float3 _OrificeWorldPosition;
		uniform float3 _PenetratorUp;
		uniform float3 _PenetratorForward;
		uniform float _PenetratorBulgePercentage;
		uniform float _PenetratorSquishPullAmount;
		uniform float _PenetratorCumProgress;
		uniform float _PenetratorCumActive;
		uniform float _NoBlendshapes;
		uniform float _OrificeLength;
		uniform float _ClipDick;
		uniform float _InvisibleWhenInside;
		uniform float3 _PenetratorRight;
		uniform float3 _OrificeWorldNormal;
		uniform float3 _OrificeOutWorldPosition1;
		uniform float3 _OrificeOutWorldPosition2;
		uniform float3 _OrificeOutWorldPosition3;
		uniform float _DeformBalls;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _MaskMap;
		uniform float4 _MaskMap_ST;
		uniform float _Cutoff = 0.5;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertexNormal = v.normal.xyz;
			float3 VertexNormal259_g979 = ase_vertexNormal;
			float3 normalizeResult27_g1039 = normalize( VertexNormal259_g979 );
			float3 temp_output_35_0_g979 = normalizeResult27_g1039;
			float4 ase_vertexTangent = v.tangent;
			float3 normalizeResult31_g1039 = normalize( ase_vertexTangent.xyz );
			float3 normalizeResult29_g1039 = normalize( cross( normalizeResult27_g1039 , normalizeResult31_g1039 ) );
			float3 temp_output_35_1_g979 = cross( normalizeResult29_g1039 , normalizeResult27_g1039 );
			float3 temp_output_35_2_g979 = normalizeResult29_g1039;
			float3 SquishDelta85_g979 = ( ( ( temp_output_35_0_g979 * v.texcoord2.x ) + ( temp_output_35_1_g979 * v.texcoord2.y ) + ( temp_output_35_2_g979 * v.texcoord2.z ) ) * _PenetratorBlendshapeMultiplier );
			float temp_output_234_0_g979 = length( SquishDelta85_g979 );
			float temp_output_11_0_g979 = max( _PenetrationDepth , 0.0 );
			float VisibleLength25_g979 = ( _PenetratorLength * ( 1.0 - temp_output_11_0_g979 ) );
			float3 DickOrigin16_g979 = _PenetratorOrigin;
			float4 appendResult132_g979 = (float4(_OrificeWorldPosition , 1.0));
			float4 transform140_g979 = mul(unity_WorldToObject,appendResult132_g979);
			float3 OrifacePosition170_g979 = (transform140_g979).xyz;
			float DickLength19_g979 = _PenetratorLength;
			float3 DickUp172_g979 = _PenetratorUp;
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 VertexPosition254_g979 = ase_vertex3Pos;
			float3 temp_output_27_0_g979 = ( VertexPosition254_g979 - DickOrigin16_g979 );
			float3 DickForward18_g979 = _PenetratorForward;
			float dotResult42_g979 = dot( temp_output_27_0_g979 , DickForward18_g979 );
			float BulgePercentage37_g979 = _PenetratorBulgePercentage;
			float temp_output_1_0_g1046 = saturate( ( abs( ( dotResult42_g979 - VisibleLength25_g979 ) ) / ( DickLength19_g979 * BulgePercentage37_g979 ) ) );
			float temp_output_94_0_g979 = sqrt( ( 1.0 - ( temp_output_1_0_g1046 * temp_output_1_0_g1046 ) ) );
			float3 PullDelta91_g979 = ( ( ( temp_output_35_0_g979 * v.texcoord3.x ) + ( temp_output_35_1_g979 * v.texcoord3.y ) + ( temp_output_35_2_g979 * v.texcoord3.z ) ) * _PenetratorBlendshapeMultiplier );
			float dotResult32_g979 = dot( temp_output_27_0_g979 , DickForward18_g979 );
			float temp_output_1_0_g1040 = saturate( ( abs( ( dotResult32_g979 - ( DickLength19_g979 * _PenetratorCumProgress ) ) ) / ( DickLength19_g979 * BulgePercentage37_g979 ) ) );
			float3 CumDelta90_g979 = ( ( ( temp_output_35_0_g979 * v.texcoord1.w ) + ( temp_output_35_1_g979 * v.texcoord2.w ) + ( temp_output_35_2_g979 * v.texcoord3.w ) ) * _PenetratorBlendshapeMultiplier );
			float3 lerpResult410_g979 = lerp( ( VertexPosition254_g979 + ( SquishDelta85_g979 * temp_output_94_0_g979 * saturate( -_PenetratorSquishPullAmount ) ) + ( temp_output_94_0_g979 * PullDelta91_g979 * saturate( _PenetratorSquishPullAmount ) ) + ( sqrt( ( 1.0 - ( temp_output_1_0_g1040 * temp_output_1_0_g1040 ) ) ) * CumDelta90_g979 * _PenetratorCumActive ) ) , VertexPosition254_g979 , _NoBlendshapes);
			float dotResult118_g979 = dot( ( lerpResult410_g979 - DickOrigin16_g979 ) , DickForward18_g979 );
			float PenetrationDepth39_g979 = _PenetrationDepth;
			float temp_output_65_0_g979 = ( PenetrationDepth39_g979 * DickLength19_g979 );
			float OrifaceLength34_g979 = _OrificeLength;
			float temp_output_73_0_g979 = ( 0.25 * OrifaceLength34_g979 );
			float dotResult80_g979 = dot( ( lerpResult410_g979 - DickOrigin16_g979 ) , DickForward18_g979 );
			float temp_output_112_0_g979 = ( -( ( ( temp_output_65_0_g979 - temp_output_73_0_g979 ) + dotResult80_g979 ) - DickLength19_g979 ) * 10.0 );
			float ClipDick413_g979 = _ClipDick;
			float lerpResult411_g979 = lerp( max( temp_output_112_0_g979 , ( ( ( temp_output_65_0_g979 + dotResult80_g979 + temp_output_73_0_g979 ) - ( OrifaceLength34_g979 + DickLength19_g979 ) ) * 10.0 ) ) , temp_output_112_0_g979 , ClipDick413_g979);
			float InsideLerp123_g979 = saturate( lerpResult411_g979 );
			float3 lerpResult124_g979 = lerp( ( ( DickForward18_g979 * dotResult118_g979 ) + DickOrigin16_g979 ) , lerpResult410_g979 , InsideLerp123_g979);
			float InvisibleWhenInside420_g979 = _InvisibleWhenInside;
			float3 lerpResult422_g979 = lerp( lerpResult410_g979 , lerpResult124_g979 , InvisibleWhenInside420_g979);
			float3 temp_output_354_0_g979 = ( lerpResult422_g979 - DickOrigin16_g979 );
			float dotResult373_g979 = dot( DickUp172_g979 , temp_output_354_0_g979 );
			float3 DickRight184_g979 = _PenetratorRight;
			float dotResult374_g979 = dot( DickRight184_g979 , temp_output_354_0_g979 );
			float dotResult375_g979 = dot( temp_output_354_0_g979 , DickForward18_g979 );
			float3 lerpResult343_g979 = lerp( ( ( ( saturate( ( ( VisibleLength25_g979 - distance( DickOrigin16_g979 , OrifacePosition170_g979 ) ) / DickLength19_g979 ) ) + 1.0 ) * dotResult373_g979 * DickUp172_g979 ) + ( ( saturate( ( ( VisibleLength25_g979 - distance( DickOrigin16_g979 , OrifacePosition170_g979 ) ) / DickLength19_g979 ) ) + 1.0 ) * dotResult374_g979 * DickRight184_g979 ) + ( DickForward18_g979 * dotResult375_g979 ) + DickOrigin16_g979 ) , lerpResult422_g979 , saturate( PenetrationDepth39_g979 ));
			float3 originalPosition126_g979 = lerpResult343_g979;
			float dotResult177_g979 = dot( ( originalPosition126_g979 - DickOrigin16_g979 ) , DickForward18_g979 );
			float temp_output_178_0_g979 = max( VisibleLength25_g979 , 0.05 );
			float temp_output_42_0_g1041 = ( dotResult177_g979 / temp_output_178_0_g979 );
			float temp_output_26_0_g1042 = temp_output_42_0_g1041;
			float temp_output_19_0_g1042 = ( 1.0 - temp_output_26_0_g1042 );
			float3 temp_output_8_0_g1041 = DickOrigin16_g979;
			float temp_output_393_0_g979 = distance( DickOrigin16_g979 , OrifacePosition170_g979 );
			float temp_output_396_0_g979 = min( temp_output_178_0_g979 , temp_output_393_0_g979 );
			float3 temp_output_9_0_g1041 = ( DickOrigin16_g979 + ( DickForward18_g979 * temp_output_396_0_g979 * 0.25 ) );
			float4 appendResult130_g979 = (float4(_OrificeWorldNormal , 0.0));
			float4 transform135_g979 = mul(unity_WorldToObject,appendResult130_g979);
			float3 OrifaceNormal155_g979 = (transform135_g979).xyz;
			float3 temp_output_10_0_g1041 = ( OrifacePosition170_g979 + ( OrifaceNormal155_g979 * 0.25 * temp_output_396_0_g979 ) );
			float3 temp_output_11_0_g1041 = OrifacePosition170_g979;
			float temp_output_1_0_g1044 = temp_output_42_0_g1041;
			float temp_output_8_0_g1044 = ( 1.0 - temp_output_1_0_g1044 );
			float3 temp_output_3_0_g1044 = temp_output_9_0_g1041;
			float3 temp_output_4_0_g1044 = temp_output_10_0_g1041;
			float3 temp_output_7_0_g1043 = ( ( 3.0 * temp_output_8_0_g1044 * temp_output_8_0_g1044 * ( temp_output_3_0_g1044 - temp_output_8_0_g1041 ) ) + ( 6.0 * temp_output_8_0_g1044 * temp_output_1_0_g1044 * ( temp_output_4_0_g1044 - temp_output_3_0_g1044 ) ) + ( 3.0 * temp_output_1_0_g1044 * temp_output_1_0_g1044 * ( temp_output_11_0_g1041 - temp_output_4_0_g1044 ) ) );
			float3 normalizeResult27_g1045 = normalize( temp_output_7_0_g1043 );
			float3 temp_output_4_0_g1041 = DickUp172_g979;
			float3 temp_output_10_0_g1043 = temp_output_4_0_g1041;
			float3 temp_output_3_0_g1041 = DickForward18_g979;
			float3 temp_output_13_0_g1043 = temp_output_3_0_g1041;
			float dotResult33_g1043 = dot( temp_output_7_0_g1043 , temp_output_10_0_g1043 );
			float3 lerpResult34_g1043 = lerp( temp_output_10_0_g1043 , -temp_output_13_0_g1043 , saturate( dotResult33_g1043 ));
			float dotResult37_g1043 = dot( temp_output_7_0_g1043 , -temp_output_10_0_g1043 );
			float3 lerpResult40_g1043 = lerp( lerpResult34_g1043 , temp_output_13_0_g1043 , saturate( dotResult37_g1043 ));
			float3 normalizeResult42_g1043 = normalize( lerpResult40_g1043 );
			float3 normalizeResult31_g1045 = normalize( normalizeResult42_g1043 );
			float3 normalizeResult29_g1045 = normalize( cross( normalizeResult27_g1045 , normalizeResult31_g1045 ) );
			float3 temp_output_65_22_g1041 = normalizeResult29_g1045;
			float3 temp_output_2_0_g1041 = ( originalPosition126_g979 - DickOrigin16_g979 );
			float3 temp_output_5_0_g1041 = DickRight184_g979;
			float dotResult15_g1041 = dot( temp_output_2_0_g1041 , temp_output_5_0_g1041 );
			float3 temp_output_65_0_g1041 = cross( normalizeResult29_g1045 , normalizeResult27_g1045 );
			float dotResult18_g1041 = dot( temp_output_2_0_g1041 , temp_output_4_0_g1041 );
			float dotResult142_g979 = dot( ( originalPosition126_g979 - DickOrigin16_g979 ) , DickForward18_g979 );
			float temp_output_152_0_g979 = ( dotResult142_g979 - VisibleLength25_g979 );
			float temp_output_157_0_g979 = ( temp_output_152_0_g979 / OrifaceLength34_g979 );
			float lerpResult416_g979 = lerp( temp_output_157_0_g979 , min( temp_output_157_0_g979 , 1.0 ) , ClipDick413_g979);
			float temp_output_42_0_g1047 = lerpResult416_g979;
			float temp_output_26_0_g1048 = temp_output_42_0_g1047;
			float temp_output_19_0_g1048 = ( 1.0 - temp_output_26_0_g1048 );
			float3 temp_output_8_0_g1047 = OrifacePosition170_g979;
			float4 appendResult145_g979 = (float4(_OrificeOutWorldPosition1 , 1.0));
			float4 transform151_g979 = mul(unity_WorldToObject,appendResult145_g979);
			float3 OrifaceOutPosition1183_g979 = (transform151_g979).xyz;
			float3 temp_output_9_0_g1047 = OrifaceOutPosition1183_g979;
			float4 appendResult144_g979 = (float4(_OrificeOutWorldPosition2 , 1.0));
			float4 transform154_g979 = mul(unity_WorldToObject,appendResult144_g979);
			float3 OrifaceOutPosition2182_g979 = (transform154_g979).xyz;
			float3 temp_output_10_0_g1047 = OrifaceOutPosition2182_g979;
			float4 appendResult143_g979 = (float4(_OrificeOutWorldPosition3 , 1.0));
			float4 transform147_g979 = mul(unity_WorldToObject,appendResult143_g979);
			float3 OrifaceOutPosition3175_g979 = (transform147_g979).xyz;
			float3 temp_output_11_0_g1047 = OrifaceOutPosition3175_g979;
			float temp_output_1_0_g1050 = temp_output_42_0_g1047;
			float temp_output_8_0_g1050 = ( 1.0 - temp_output_1_0_g1050 );
			float3 temp_output_3_0_g1050 = temp_output_9_0_g1047;
			float3 temp_output_4_0_g1050 = temp_output_10_0_g1047;
			float3 temp_output_7_0_g1049 = ( ( 3.0 * temp_output_8_0_g1050 * temp_output_8_0_g1050 * ( temp_output_3_0_g1050 - temp_output_8_0_g1047 ) ) + ( 6.0 * temp_output_8_0_g1050 * temp_output_1_0_g1050 * ( temp_output_4_0_g1050 - temp_output_3_0_g1050 ) ) + ( 3.0 * temp_output_1_0_g1050 * temp_output_1_0_g1050 * ( temp_output_11_0_g1047 - temp_output_4_0_g1050 ) ) );
			float3 normalizeResult27_g1051 = normalize( temp_output_7_0_g1049 );
			float3 temp_output_4_0_g1047 = DickUp172_g979;
			float3 temp_output_10_0_g1049 = temp_output_4_0_g1047;
			float3 temp_output_3_0_g1047 = DickForward18_g979;
			float3 temp_output_13_0_g1049 = temp_output_3_0_g1047;
			float dotResult33_g1049 = dot( temp_output_7_0_g1049 , temp_output_10_0_g1049 );
			float3 lerpResult34_g1049 = lerp( temp_output_10_0_g1049 , -temp_output_13_0_g1049 , saturate( dotResult33_g1049 ));
			float dotResult37_g1049 = dot( temp_output_7_0_g1049 , -temp_output_10_0_g1049 );
			float3 lerpResult40_g1049 = lerp( lerpResult34_g1049 , temp_output_13_0_g1049 , saturate( dotResult37_g1049 ));
			float3 normalizeResult42_g1049 = normalize( lerpResult40_g1049 );
			float3 normalizeResult31_g1051 = normalize( normalizeResult42_g1049 );
			float3 normalizeResult29_g1051 = normalize( cross( normalizeResult27_g1051 , normalizeResult31_g1051 ) );
			float3 temp_output_65_22_g1047 = normalizeResult29_g1051;
			float3 temp_output_2_0_g1047 = ( originalPosition126_g979 - DickOrigin16_g979 );
			float3 temp_output_5_0_g1047 = DickRight184_g979;
			float dotResult15_g1047 = dot( temp_output_2_0_g1047 , temp_output_5_0_g1047 );
			float3 temp_output_65_0_g1047 = cross( normalizeResult29_g1051 , normalizeResult27_g1051 );
			float dotResult18_g1047 = dot( temp_output_2_0_g1047 , temp_output_4_0_g1047 );
			float temp_output_208_0_g979 = saturate( sign( temp_output_152_0_g979 ) );
			float3 lerpResult221_g979 = lerp( ( ( ( temp_output_19_0_g1042 * temp_output_19_0_g1042 * temp_output_19_0_g1042 * temp_output_8_0_g1041 ) + ( temp_output_19_0_g1042 * temp_output_19_0_g1042 * 3.0 * temp_output_26_0_g1042 * temp_output_9_0_g1041 ) + ( 3.0 * temp_output_19_0_g1042 * temp_output_26_0_g1042 * temp_output_26_0_g1042 * temp_output_10_0_g1041 ) + ( temp_output_26_0_g1042 * temp_output_26_0_g1042 * temp_output_26_0_g1042 * temp_output_11_0_g1041 ) ) + ( temp_output_65_22_g1041 * dotResult15_g1041 ) + ( temp_output_65_0_g1041 * dotResult18_g1041 ) ) , ( ( ( temp_output_19_0_g1048 * temp_output_19_0_g1048 * temp_output_19_0_g1048 * temp_output_8_0_g1047 ) + ( temp_output_19_0_g1048 * temp_output_19_0_g1048 * 3.0 * temp_output_26_0_g1048 * temp_output_9_0_g1047 ) + ( 3.0 * temp_output_19_0_g1048 * temp_output_26_0_g1048 * temp_output_26_0_g1048 * temp_output_10_0_g1047 ) + ( temp_output_26_0_g1048 * temp_output_26_0_g1048 * temp_output_26_0_g1048 * temp_output_11_0_g1047 ) ) + ( temp_output_65_22_g1047 * dotResult15_g1047 ) + ( temp_output_65_0_g1047 * dotResult18_g1047 ) ) , temp_output_208_0_g979);
			float3 temp_output_42_0_g1052 = DickForward18_g979;
			float NonVisibleLength165_g979 = ( temp_output_11_0_g979 * _PenetratorLength );
			float3 temp_output_52_0_g1052 = ( ( temp_output_42_0_g1052 * ( ( NonVisibleLength165_g979 - OrifaceLength34_g979 ) - DickLength19_g979 ) ) + ( originalPosition126_g979 - DickOrigin16_g979 ) );
			float dotResult53_g1052 = dot( temp_output_42_0_g1052 , temp_output_52_0_g1052 );
			float temp_output_1_0_g1054 = 1.0;
			float temp_output_8_0_g1054 = ( 1.0 - temp_output_1_0_g1054 );
			float3 temp_output_3_0_g1054 = OrifaceOutPosition1183_g979;
			float3 temp_output_4_0_g1054 = OrifaceOutPosition2182_g979;
			float3 temp_output_7_0_g1053 = ( ( 3.0 * temp_output_8_0_g1054 * temp_output_8_0_g1054 * ( temp_output_3_0_g1054 - OrifacePosition170_g979 ) ) + ( 6.0 * temp_output_8_0_g1054 * temp_output_1_0_g1054 * ( temp_output_4_0_g1054 - temp_output_3_0_g1054 ) ) + ( 3.0 * temp_output_1_0_g1054 * temp_output_1_0_g1054 * ( OrifaceOutPosition3175_g979 - temp_output_4_0_g1054 ) ) );
			float3 normalizeResult27_g1055 = normalize( temp_output_7_0_g1053 );
			float3 temp_output_85_23_g1052 = normalizeResult27_g1055;
			float3 temp_output_4_0_g1052 = DickUp172_g979;
			float dotResult54_g1052 = dot( temp_output_4_0_g1052 , temp_output_52_0_g1052 );
			float3 temp_output_10_0_g1053 = temp_output_4_0_g1052;
			float3 temp_output_13_0_g1053 = temp_output_42_0_g1052;
			float dotResult33_g1053 = dot( temp_output_7_0_g1053 , temp_output_10_0_g1053 );
			float3 lerpResult34_g1053 = lerp( temp_output_10_0_g1053 , -temp_output_13_0_g1053 , saturate( dotResult33_g1053 ));
			float dotResult37_g1053 = dot( temp_output_7_0_g1053 , -temp_output_10_0_g1053 );
			float3 lerpResult40_g1053 = lerp( lerpResult34_g1053 , temp_output_13_0_g1053 , saturate( dotResult37_g1053 ));
			float3 normalizeResult42_g1053 = normalize( lerpResult40_g1053 );
			float3 normalizeResult31_g1055 = normalize( normalizeResult42_g1053 );
			float3 normalizeResult29_g1055 = normalize( cross( normalizeResult27_g1055 , normalizeResult31_g1055 ) );
			float3 temp_output_85_0_g1052 = cross( normalizeResult29_g1055 , normalizeResult27_g1055 );
			float3 temp_output_43_0_g1052 = DickRight184_g979;
			float dotResult55_g1052 = dot( temp_output_43_0_g1052 , temp_output_52_0_g1052 );
			float3 temp_output_85_22_g1052 = normalizeResult29_g1055;
			float temp_output_222_0_g979 = saturate( sign( ( temp_output_157_0_g979 - 1.0 ) ) );
			float3 lerpResult224_g979 = lerp( lerpResult221_g979 , ( ( ( dotResult53_g1052 * temp_output_85_23_g1052 ) + ( dotResult54_g1052 * temp_output_85_0_g1052 ) + ( dotResult55_g1052 * temp_output_85_22_g1052 ) ) + OrifaceOutPosition3175_g979 ) , temp_output_222_0_g979);
			float3 lerpResult418_g979 = lerp( lerpResult224_g979 , lerpResult221_g979 , ClipDick413_g979);
			float temp_output_226_0_g979 = saturate( -PenetrationDepth39_g979 );
			float3 lerpResult232_g979 = lerp( lerpResult418_g979 , originalPosition126_g979 , temp_output_226_0_g979);
			float3 ifLocalVar237_g979 = 0;
			if( temp_output_234_0_g979 <= 0.0 )
				ifLocalVar237_g979 = originalPosition126_g979;
			else
				ifLocalVar237_g979 = lerpResult232_g979;
			float DeformBalls426_g979 = _DeformBalls;
			float3 lerpResult428_g979 = lerp( ifLocalVar237_g979 , lerpResult232_g979 , DeformBalls426_g979);
			v.vertex.xyz = lerpResult428_g979;
			v.vertex.w = 1;
			float3 temp_output_21_0_g1041 = VertexNormal259_g979;
			float dotResult55_g1041 = dot( temp_output_21_0_g1041 , temp_output_3_0_g1041 );
			float dotResult56_g1041 = dot( temp_output_21_0_g1041 , temp_output_4_0_g1041 );
			float dotResult57_g1041 = dot( temp_output_21_0_g1041 , temp_output_5_0_g1041 );
			float3 normalizeResult31_g1041 = normalize( ( ( dotResult55_g1041 * normalizeResult27_g1045 ) + ( dotResult56_g1041 * temp_output_65_0_g1041 ) + ( dotResult57_g1041 * temp_output_65_22_g1041 ) ) );
			float3 temp_output_21_0_g1047 = VertexNormal259_g979;
			float dotResult55_g1047 = dot( temp_output_21_0_g1047 , temp_output_3_0_g1047 );
			float dotResult56_g1047 = dot( temp_output_21_0_g1047 , temp_output_4_0_g1047 );
			float dotResult57_g1047 = dot( temp_output_21_0_g1047 , temp_output_5_0_g1047 );
			float3 normalizeResult31_g1047 = normalize( ( ( dotResult55_g1047 * normalizeResult27_g1051 ) + ( dotResult56_g1047 * temp_output_65_0_g1047 ) + ( dotResult57_g1047 * temp_output_65_22_g1047 ) ) );
			float3 lerpResult227_g979 = lerp( normalizeResult31_g1041 , normalizeResult31_g1047 , temp_output_208_0_g979);
			float3 temp_output_24_0_g1052 = VertexNormal259_g979;
			float dotResult61_g1052 = dot( temp_output_42_0_g1052 , temp_output_24_0_g1052 );
			float dotResult62_g1052 = dot( temp_output_4_0_g1052 , temp_output_24_0_g1052 );
			float dotResult60_g1052 = dot( temp_output_43_0_g1052 , temp_output_24_0_g1052 );
			float3 normalizeResult33_g1052 = normalize( ( ( dotResult61_g1052 * temp_output_85_23_g1052 ) + ( dotResult62_g1052 * temp_output_85_0_g1052 ) + ( dotResult60_g1052 * temp_output_85_22_g1052 ) ) );
			float3 lerpResult233_g979 = lerp( lerpResult227_g979 , normalizeResult33_g1052 , temp_output_222_0_g979);
			float3 lerpResult419_g979 = lerp( lerpResult233_g979 , lerpResult227_g979 , ClipDick413_g979);
			float3 lerpResult238_g979 = lerp( lerpResult419_g979 , VertexNormal259_g979 , temp_output_226_0_g979);
			float3 ifLocalVar391_g979 = 0;
			if( temp_output_234_0_g979 <= 0.0 )
				ifLocalVar391_g979 = VertexNormal259_g979;
			else
				ifLocalVar391_g979 = lerpResult238_g979;
			v.normal = ifLocalVar391_g979;
			float lerpResult424_g979 = lerp( 1.0 , InsideLerp123_g979 , InvisibleWhenInside420_g979);
			o.vertexToFrag250_g979 = lerpResult424_g979;
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
			clip( i.vertexToFrag250_g979 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18912
996;295;1696;809;-5880.022;2097.915;2.675143;True;False
Node;AmplifyShaderEditor.SamplerNode;100;7108.375,-1682.497;Inherit;True;Property;_MainTex;MainTex;23;0;Create;True;0;0;0;False;0;False;-1;None;a7fd0f05ef2dbbd43ac64dff86a86708;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;102;7105.585,-1265.636;Inherit;True;Property;_MaskMap;MaskMap;24;0;Create;True;0;0;0;False;0;False;-1;None;b6ba1a4a299c969449e3ec084c188366;True;0;False;gray;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;101;7104.496,-1467.525;Inherit;True;Property;_BumpMap;BumpMap;25;0;Create;True;0;0;0;False;0;False;-1;None;22df23dcaaa4c974888f14b0f36d484f;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;676;7008.464,-1016.773;Inherit;False;PenetrationTechDeformation;0;;979;cb4db099da64a8846a0c6877ff8e2b5f;0;3;253;FLOAT3;0,0,0;False;258;FLOAT3;0,0,0;False;265;FLOAT3;0,0,0;False;3;FLOAT3;0;FLOAT;251;FLOAT3;252
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;552;7623.605,-1459.329;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;PenetrationTech/Standard/Penetrator;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;TransparentCutout;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Absolute;0;;22;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;552;0;100;0
WireConnection;552;1;101;0
WireConnection;552;3;102;1
WireConnection;552;4;102;4
WireConnection;552;10;676;251
WireConnection;552;11;676;0
WireConnection;552;12;676;252
ASEEND*/
//CHKSM=C742848623220A3B279AA07AFACFDA68A847C69C