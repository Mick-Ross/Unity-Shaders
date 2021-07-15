// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/WhitePlane"
{
    Properties
    {
        _RippleTex("Ripple Texture", 2D) = "white" {}
        _Strength("Distortion Strength", float) = 1.0
        _Speed("Ripple Speed", float) = 1.0
        _xScale("X Strength", Range(0.0, 1.0)) = 1.0
        _yScale("Y Strength", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "DisableBatching" = "True"
        } 

        GrabPass
        {
            "_BackgroundTexture"
        }

        Pass
        {
            CGPROGRAM
            
            #pragma vertex MyVertexProgram
            #pragma fragment MyFragmentProgram
            #include "UnityCG.cginc"

            sampler2D _BackgroundTexture;
            sampler2D _RippleTex;
            float _Strength;
            float _Speed;
            float _xScale;
            float _yScale;

            struct vertexInput
            {
                float4 vertex : POSITION;
                float3 texCoord : TEXCOORD0;
            };

            struct vertexOutput
            {
                float4 pos : SV_POSITION;
                float4 grabPos : TEXCOORD0;
            };

            vertexOutput MyVertexProgram(vertexInput input) {
                vertexOutput output;
                /*
                output.pos = UnityObjectToClipPos(input.vertex);
                output.grabPos = float4(input.texCoord.x, input.texCoord.y, input.texCoord.z, 1);
                */

                //This correctly takes the view behind the object 
                // and projects it to the screen space
                output.pos = UnityObjectToClipPos(input.vertex);
                output.grabPos = ComputeGrabScreenPos(output.pos);

                // distort in x and y direction
                float noise = tex2Dlod(_RippleTex, float4(input.texCoord, 0)).rgb;

                // with just trig fxns settings: -0.2, 101.2, 1, 1
                // with just smoothstep (multiply): 0.02, 96.52, 1, 1
                
                // regular trig fxns
                output.grabPos.x += cos(noise*_Time.x*_Speed) * _Strength*0.001 * _xScale;
                output.grabPos.y += sin(noise*_Time.x*_Speed) * _Strength*0.001 * _yScale; 

                // smoothstep
                output.grabPos.x += (-1 + 2*smoothstep(-1, 1, cos(noise*_Time.x*_Speed))) *_Strength * _xScale; 
                output.grabPos.y += (-1 + 2*smoothstep(-1, 1, sin(noise*_Time.x*_Speed))) *_Strength * _xScale;

                return output;
            }

            float4 MyFragmentProgram(vertexOutput input) : COLOR {
                return tex2Dproj(_BackgroundTexture, input.grabPos);
            }

            ENDCG
        }
    }
}
