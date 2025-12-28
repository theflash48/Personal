/// @description Inserte aquí la descripción
// Puede escribir su código en este editor

if (_ini_pos + _desplazamiento <= x )
{
	_custom_speed *= -1;
}
if (_ini_pos - _desplazamiento >= x )
{
	_custom_speed *= -1;
}
x += _custom_speed * delta_time;