import { useEffect, useRef, useState } from 'react';
import { AnimatePresence, m, useReducedMotion } from 'motion/react';
import { Check, X } from 'lucide-react';
import { mensajeDeError } from '../../api/cliente';
import Boton from '../../componentes/Boton';
import { resortes, tokensMovimiento } from '../../utilidades/movimiento';
import { useRegistrarCumplimiento } from './consultas';

// Registro de hoy con dos decisiones explícitas: «Hecho» o «No lo hice» (esta rompe la racha). Una
// vez registrado se muestra el estado con «Cambiar», porque se puede corregir el mismo día. Al
// marcar «Hecho», la insignia entra con un pequeño rebote.
export default function BotonesRegistro({ habito }) {
  const registrar = useRegistrarCumplimiento();
  const [cambiando, setCambiando] = useState(false);
  const botonCambiar = useRef(null);
  const enfocarCambiar = useRef(false);
  const reducir = useReducedMotion();
  const registrado = habito.cumplidoHoy !== null && habito.cumplidoHoy !== undefined;
  const enviando = registrar.isPending ? registrar.variables?.cumplido : null;

  // Tras registrar, los botones se reemplazan por el estado: el foco pasa a «Cambiar» para no perderse.
  useEffect(() => {
    if (enfocarCambiar.current && botonCambiar.current) {
      enfocarCambiar.current = false;
      botonCambiar.current.focus();
    }
  });

  function marcar(cumplido) {
    registrar.mutate(
      { id: habito.id, cumplido },
      {
        onSuccess: () => {
          enfocarCambiar.current = true;
          setCambiando(false);
        },
      },
    );
  }

  const aparecer = {
    initial: { opacity: 0, scale: reducir ? 1 : tokensMovimiento.escala.presion },
    animate: { opacity: 1, scale: 1 },
    exit: { opacity: 0 },
    transition: reducir ? { duration: tokensMovimiento.duracion.rapida } : resortes.rebote,
  };

  return (
    <div className="relative flex flex-col gap-1.5">
      {/* popLayout: el estado nuevo se monta al instante (el foco puede ir a «Cambiar») y el que sale
          deja de ocupar lugar mientras se desvanece. */}
      <AnimatePresence mode="popLayout" initial={false}>
        {registrado && !cambiando ? (
          <m.div key="estado" {...aparecer} className="flex flex-wrap items-center gap-2 text-sm" role="status">
            {habito.cumplidoHoy ? (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-exito-suave px-3 py-1 font-semibold text-exito">
                <Check className="size-4" strokeWidth={2.6} aria-hidden="true" />
                Hecho
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-peligro-suave px-3 py-1 font-semibold text-peligro">
                <X className="size-4" strokeWidth={2.6} aria-hidden="true" />
                No lo hiciste
              </span>
            )}
            <button
              ref={botonCambiar}
              type="button"
              onClick={() => setCambiando(true)}
              className="rounded-lg px-1.5 py-0.5 text-suave underline-offset-2 hover:text-texto hover:underline"
              aria-label={`Cambiar el registro de hoy de ${habito.nombre}`}
            >
              Cambiar
            </button>
          </m.div>
        ) : (
          <m.div key="botones" {...aparecer} className="flex flex-wrap gap-2" role="group" aria-label={`Registrar hoy: ${habito.nombre}`}>
            <Boton tamano="sm" onClick={() => marcar(true)} cargando={enviando === true} disabled={registrar.isPending}>
              {enviando !== true && <Check className="size-4" aria-hidden="true" />}
              Hecho
            </Boton>
            <Boton tamano="sm" variante="secundario" onClick={() => marcar(false)} cargando={enviando === false} disabled={registrar.isPending}>
              {enviando !== false && <X className="size-4" aria-hidden="true" />}
              No lo hice
            </Boton>
            {cambiando && (
              <Boton tamano="sm" variante="fantasma" onClick={() => setCambiando(false)} disabled={registrar.isPending}>
                Cancelar
              </Boton>
            )}
          </m.div>
        )}
      </AnimatePresence>
      {registrar.isError && (
        <p role="alert" className="max-w-xs text-xs font-medium text-peligro">
          {mensajeDeError(registrar.error)}
        </p>
      )}
    </div>
  );
}
