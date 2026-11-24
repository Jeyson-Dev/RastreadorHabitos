// Barra de avance accesible (progressbar con valor y texto). «color» es una clase de fondo.
export default function BarraAvance({ valor, maximo, etiqueta, color = 'bg-acento', className = '' }) {
  const porcentaje = maximo > 0 ? Math.min(100, Math.round((valor / maximo) * 100)) : 0;
  return (
    <div
      role="progressbar"
      aria-label={etiqueta}
      aria-valuemin={0}
      aria-valuemax={maximo}
      aria-valuenow={Math.min(valor, maximo)}
      aria-valuetext={`${valor} de ${maximo}`}
      className={`h-2.5 overflow-hidden rounded-full bg-superficie-suave ${className}`}
    >
      <div className={`h-full rounded-full transition-[width] duration-500 ${color}`} style={{ width: `${porcentaje}%` }} />
    </div>
  );
}
