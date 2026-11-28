import { useReducedMotion } from 'motion/react';

// Único lugar con duraciones, curvas, distancias y resortes de las animaciones (skill
// motion-foundations). Ningún componente escribe estos valores a mano.
export const tokensMovimiento = {
  duracion: { instante: 0.08, rapida: 0.18, normal: 0.35, lenta: 0.6 },
  curva: { suave: [0.22, 1, 0.36, 1], firme: [0.4, 0, 0.2, 1] },
  distancia: { xs: 4, sm: 8, md: 16, lg: 24 },
  escala: { sutil: 0.98, presion: 0.95, salto: 1.06 },
  escalonado: 0.05,
};

export const resortes = {
  agil: { type: 'spring', stiffness: 300, damping: 30 },
  suave: { type: 'spring', stiffness: 120, damping: 14 },
  rebote: { type: 'spring', stiffness: 400, damping: 12 },
  instante: { type: 'spring', stiffness: 600, damping: 35 },
};

// Entrada al montar: sube y aparece. Con «reducir movimiento» solo hay un fundido breve, sin
// desplazamiento (la app es una SPA sin render en servidor: no hay riesgo de hidratación).
export function useEntrada(distancia = tokensMovimiento.distancia.md) {
  const reducir = useReducedMotion();
  return {
    initial: { opacity: 0, y: reducir ? 0 : distancia },
    animate: { opacity: 1, y: 0 },
    transition: reducir
      ? { duration: tokensMovimiento.duracion.rapida }
      : { duration: tokensMovimiento.duracion.normal, ease: tokensMovimiento.curva.suave },
  };
}

// Contenedor que escalona la entrada de sus hijos (listas y rejillas).
export function useEscalonado() {
  const reducir = useReducedMotion();
  return {
    contenedor: {
      initial: 'oculto',
      animate: 'visible',
      variants: { oculto: {}, visible: { transition: { staggerChildren: reducir ? 0 : tokensMovimiento.escalonado } } },
    },
    elemento: {
      variants: {
        oculto: { opacity: 0, y: reducir ? 0 : tokensMovimiento.distancia.sm },
        visible: {
          opacity: 1,
          y: 0,
          transition: reducir ? { duration: tokensMovimiento.duracion.rapida } : resortes.agil,
        },
      },
    },
  };
}
