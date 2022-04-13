

fixed tri( fixed f ){
	return smoothstep( 0., 1., abs(.5-frac(f))*2. );
}

fixed3 ApplyWind( fixed time, fixed3 pos, fixed3 strength ){
	fixed3 offset;
	offset.x = tri( time+pos.x );
	offset.y = tri( time+pos.y+_SinTime.z );
	offset.z = tri( time+pos.z+_CosTime.w );
	return pos+offset*strength;
}

fixed zigZagNoise( fixed2 pos ){
	fixed o1 = abs(frac(pos.x)-.5)*2.;
	return abs(frac(pos.y+o1)-.5)*2.;
}