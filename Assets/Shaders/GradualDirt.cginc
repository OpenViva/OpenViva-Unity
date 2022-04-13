
fixed3 ApplyGradualDirt( fixed3 col, sampler2D dirtTex, fixed amount, fixed2 uv ){

	fixed4 dirt = tex2D(dirtTex,uv*7.0);
	col = lerp( col, dirt, smoothstep( 0., dirt.a, amount )*0.9 );
	return col;
}

#define APPLY_GRADUAL_DIRT(col,tex,amount,uv) col = ApplyGradualDirt( col, tex, amount, uv );