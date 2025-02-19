Shader "Custom/FootZoneColor_Branchless"
{
    Properties
    {
        _LeftColor ("Left Color", Color) = (0,1,0,1)
        _RightColor ("Right Color", Color) = (0,1,0,1)
        _TopColor ("Toe (Front) Color", Color) = (0,1,0,1)
        _BottomColor ("Heel (Back) Color", Color) = (0,1,0,1)
        _MainTex ("Base Texture", 2D) = "white" {}
        // Thresholds for region sizes (adjust these to fit your UV layout)
        _LeftEdge ("Left Edge Width", Range(0.0,0.5)) = 0.1
        _RightEdge ("Right Edge Width", Range(0.0,0.5)) = 0.1
        _ToeEdge ("Toe Region Height", Range(0.0,1.0)) = 0.2
        _HeelEdge ("Heel Region Height", Range(0.0,1.0)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _LeftColor;
            fixed4 _RightColor;
            fixed4 _TopColor;
            fixed4 _BottomColor;
            float _LeftEdge;
            float _RightEdge;
            float _ToeEdge;
            float _HeelEdge;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Apply texture scale/offset. If you prefer to work directly in UV space,
                // you can simply use: o.uv = v.uv;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 texColor = tex2D(_MainTex, uv);
                
                // Compute masks using step functions:
                // leftMask is 1 if uv.x is less than _LeftEdge, 0 otherwise.
                float leftMask = 1.0 - step(_LeftEdge, uv.x);
                // rightMask is 1 if uv.x is greater than or equal to (1 - _RightEdge).
                float rightMask = step(1.0 - _RightEdge, uv.x);
                // toeMask is 1 if uv.y is greater than or equal to (1 - _ToeEdge).
                float toeMask = step(1.0 - _ToeEdge, uv.y);
                // heelMask is 1 if uv.y is less than _HeelEdge.
                float heelMask = 1.0 - step(_HeelEdge, uv.y);
                
                // Enforce a priority order:
                //   1. Left zone overrides everything.
                //   2. If not left, check right.
                //   3. If neither left nor right, check toe.
                //   4. If not toe, then check heel.
                //   5. If none match, use a default color (green).
                float useLeft = leftMask;
                float useRight = (1.0 - useLeft) * rightMask;
                float useToe = (1.0 - useLeft) * (1.0 - useRight) * toeMask;
                float useHeel = (1.0 - useLeft) * (1.0 - useRight) * (1.0 - toeMask) * heelMask;
                float useDefault = 1.0 - (useLeft + useRight + useToe + useHeel);
                
                fixed4 defaultColor = fixed4(0, 1, 0, 1); // default green
                
                fixed4 zoneColor = _LeftColor   * useLeft +
                                   _RightColor  * useRight +
                                   _TopColor    * useToe +
                                   _BottomColor * useHeel +
                                   defaultColor * useDefault;
                
                return zoneColor * texColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
