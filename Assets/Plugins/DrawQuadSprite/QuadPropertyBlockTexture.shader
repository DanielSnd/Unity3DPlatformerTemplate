// This shader fills the mesh shape with a color predefined in the code.
Shader "QuadTextureMatPropBlock"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    {
        [PerRendererData] _MainColor ("Main Color", Color) = (1, 1, 1, 1)
        [PerRendererData] _BaseMap ("Base Map", 2D) = "white" {}    
    }

    // The SubShader block containing the Shader code. 
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
       
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "QuadTexturePass"

            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex vert
            // This line defines the name of the fragment shader. 
            #pragma fragment frag

            #pragma shader_feature _ALPHATEST_OFF
            #pragma shader_feature _ALPHAPREMULTIPLY_OFF
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"  
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
            float4 _MainColor;
            float4 _BaseMap_TexelSize;
            float4 _BaseMap_ST;
            TEXTURE2D(_BaseMap);
            CBUFFER_END

            SAMPLER(sampler_BaseMap);

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS   : POSITION;   
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
            // The positions in this struct must have the SV_POSITION semantic.                
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };            
            

            // The vertex shader definition with properties defined in the Varyings 
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            Varyings vert(Attributes IN)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
            
                float3 multiply_pos_scale = IN.positionOS * (
                    float3(
                        length(
                            float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)
                            ),
                            length(
                                float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)
                                ),
                            length(
                                float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)
                                )
                        )
                    );

                float position_x = multiply_pos_scale[0];
                float position_y = multiply_pos_scale[1];
                float position_z = multiply_pos_scale[2];
                float4 convert_to_float_4 = float4(position_x, position_y, position_z, float(0));
                float4 multiplied_by_matrix = mul(UNITY_MATRIX_I_V, convert_to_float_4);
                 float4x4 matObjectToWorld = GetObjectToWorldMatrix();
                float3 objpos = GetAbsolutePositionWS(float3(matObjectToWorld[0].w, matObjectToWorld[1].w, matObjectToWorld[2].w));
                float3 add_obj_pos = objpos + multiplied_by_matrix;
                float3 transform_obj_pos = TransformWorldToObject( add_obj_pos);
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous space
                OUT.positionCS = TransformObjectToHClip(transform_obj_pos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                // Returning the output.
                return OUT;
            }

            // The fragment shader definition.            
            half4 frag(Varyings IN) : SV_Target
            {
                half4 customColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _MainColor;
                customColor.a = min(_MainColor.a, customColor.a);
                return customColor;
            }
            ENDHLSL
        }
    }
}