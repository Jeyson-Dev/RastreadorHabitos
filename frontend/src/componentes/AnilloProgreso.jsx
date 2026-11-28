import { m, useReducedMotion } from 'motion/react';
import { tokensMovimiento } from '../utilidades/movimiento';

// Anillo de progreso: el trazo se dibuja hasta el porcentaje (pathLength, sin animar medidas). Lleva
// role="progressbar" con su valor; el número del centro es decorativo para no leerlo dos veces.
export default function AnilloProgreso({ valor, maximo, etiqueta, tamano = 132, grosor = 12, children }) {
  const reducir = useReducedMotion();
  const fraccion = maximo > 0 ? Math.min(valor / maximo, 1) : 0;
  const radio = (tamano - grosor) / 2;
  const completo = maximo > 0 && valor >= maximo;

  return (
    <div
      role="progressbar"
      aria-label={etiqueta}
      aria-valuemin={0}
      aria-valuemax={maximo}
      aria-valuenow={valor}
      aria-valuetext={`${valor} de ${maximo}`}
      className="relative inline-grid shrink-0 place-items-center"
      style={{ width: tamano, height: tamano }}
    >
      <svg width={tamano} height={tamano} viewBox={`0 0 ${tamano} ${tamano}`} className="-rotate-90" aria-hidden="true">
        <defs>
          <linearGradient id="anillo-degradado" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stopColor="var(--racha)" />
            <stop offset="100%" stopColor="var(--acento)" />
          </linearGradient>
        </defs>
        <circle cx={tamano / 2} cy={tamano / 2} r={radio} fill="none" stroke="var(--superficie-suave)" strokeWidth={grosor} />
        <m.circle
          cx={tamano / 2}
          cy={tamano / 2}
          r={radio}
          fill="none"
          stroke={completo ? 'var(--exito)' : 'url(#anillo-degradado)'}
          strokeWidth={grosor}
          strokeLinecap="round"
          initial={{ pathLength: reducir ? fraccion : 0 }}
          animate={{ pathLength: fraccion }}
          transition={{ duration: reducir ? 0 : tokensMovimiento.duracion.lenta, ease: tokensMovimiento.curva.suave }}
          style={{ opacity: fraccion === 0 ? 0 : 1 }}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center" aria-hidden="true">
        {children}
      </div>
    </div>
  );
}
