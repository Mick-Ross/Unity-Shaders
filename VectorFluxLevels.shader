Shader "EM/VectorFluxLevels"
{
	Properties
	{
		_MainTex ("Color (RGB) Alpha (A)", 2D) = "white" {}
		_Color ("Color", Color) = (0, 1, 0, 1)
		_Opacity ("Opacity", float) = 0.2
		_ScaleFactor ("Scale Factor", float) = 0.03
		_CutoffFlux1 ("Cutoff Flux (Outer)", float) = 0.5
        _CutoffFlux2 ("Cutoff Flux (Middle)", float) = 0.5
        _CutoffFlux3 ("Cutoff Flux (Inner)", float) = 0.5
        _albedo1 ("Albedo Middle", float) = 0.5

	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100
		
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
            
            float4 _Color;
            float _Opacity;
            float _ScaleFactor;
            float _CutoffFlux1;
            float _CutoffFlux2;
            float _CutoffFlux3;
            float _albedo1;

            float4 _Charges[32];
            //float _ChargeStrengths[32];
            int _ChargeArrayLength;
            
			struct VertexData
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct InterpolatorsVertex
			{
				float2 uv : TEXCOORD0;
				float4 worldPos: TEXCOORD1;
				float4 vertex : SV_POSITION;
				float4 normal : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			InterpolatorsVertex vert (VertexData v)
			{
				InterpolatorsVertex o;
				
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal.xyz = v.normal;
				o.normal.w = 0;
				
				return o;
			}
			
			float3 ElectricField(float3 position)
            {
                float3 field = float3(0,0,0);
                for(int j = 0; j < _ChargeArrayLength; j++)
                {
					                                               // put abs here
                	// field = field + ((position - _Charges[j].xyz) * abs(_Charges[j].w)) / (pow(distance(position, _Charges[j].xyz),2));
					field = field + /* ((position - _Charges[j].xyz) */ (1 / pow(distance(position, _Charges[j].xyz), 2));
                }
                return field;
            }
			
			fixed4 frag (InterpolatorsVertex i) : SV_Target
			{
				float4 color;
				float3 field = ElectricField(i.worldPos.xyz);
				float distanceNormalized = field * (1 - pow(0.8, field));
				
				// float flux = dot(normalize(mul(unity_ObjectToWorld,i.normal).xyz), field);
				
                // color = _Color * lerp(0, 0.5, -flux * _ScaleFactor);		
				color = _Color * smoothstep(0.5, 2.5, -distanceNormalized /* -flux */  * _ScaleFactor);	    
				
				//return color; 

				color.a = _Opacity;
				if (distanceNormalized > _CutoffFlux1) {
					color.a = 0;
				} else if (distanceNormalized < _CutoffFlux1 && distanceNormalized >= _CutoffFlux2) {
					color.a = _Opacity;
				} else {
                    color.a = 0;
                }
				
				color *= tex2D(_MainTex, i.uv);

				return color;
			}
			ENDCG
		}
	}
}
