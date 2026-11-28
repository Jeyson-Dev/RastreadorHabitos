import { useState } from 'react';
import { Check, RotateCcw, Trash2 } from 'lucide-react';
import { mensajeDeError } from '../../api/cliente';
import { ESTADOS_META } from '../../api/metas';
import BarraAvance from '../../componentes/BarraAvance';
import Boton from '../../componentes/Boton';
import MenuAcciones from '../../componentes/MenuAcciones';
import { diaLocalDe, diasEntre, fechaCorta } from '../../utilidades/fechas';
import ChipEstado from './ChipEstado';
import { PRESENTACION_ESTADO } from './presentacion';
import { useCompletarMeta } from './consultas';

function textoPlazo(meta, hoy) {
  if (meta.estado === ESTADOS_META.Completada) return `Meta del ${fechaCorta(meta.fechaInicio)} al ${fechaCorta(meta.fechaLimite)}`;
  const restantes = diasEntre(hoy, meta.fechaLimite);
  if (restantes < 0) return `Venció el ${fechaCorta(meta.fechaLimite)}`;
  if (restantes === 0) return `Vence hoy`;
  return `Vence el ${fechaCorta(meta.fechaLimite)} · ${restantes === 1 ? 'falta 1 día' : `faltan ${restantes} días`}`;
}

// Tarjeta de una meta: estado, avance, plazo y la única acción posible según la máquina de estados.
// Completar exige estar En progreso con avance ≥ objetivo (Pendiente → Completada está prohibida);
// una Fallida se reactiva con una fecha nueva. El servidor vuelve a validar todo.
export default function TarjetaMeta({ meta, nombresHabitos, alReactivar, alCompletar, alEliminar }) {
  const completar = useCompletarMeta();
  const [confirmando, setConfirmando] = useState(false);
  const hoy = diaLocalDe(new Date());
  const faltan = Math.max(meta.objetivoCumplimientos - meta.avance, 0);
  const puedeCompletar = meta.estado === ESTADOS_META.EnProgreso && faltan === 0;

  let accion = null;
  if (puedeCompletar) {
    accion = confirmando ? (
      <div className="flex flex-wrap items-center gap-2" role="group" aria-label="Confirmar que la meta está completada">
        <span className="text-sm">¿Marcar como completada? No se puede deshacer.</span>
        <Boton tamano="sm" cargando={completar.isPending} onClick={() =>
            completar.mutate(meta.id, {
              onSuccess: () => {
                setConfirmando(false);
                alCompletar?.(meta.nombre);
              },
            })
          }>
          Sí, completar
        </Boton>
        <Boton tamano="sm" variante="fantasma" onClick={() => setConfirmando(false)} disabled={completar.isPending}>
          Cancelar
        </Boton>
      </div>
    ) : (
      <Boton tamano="sm" onClick={() => setConfirmando(true)}>
        <Check className="size-4" aria-hidden="true" />
        Completar meta
      </Boton>
    );
  } else if (meta.estado === ESTADOS_META.Fallida) {
    accion = (
      <Boton tamano="sm" variante="secundario" onClick={() => alReactivar(meta)}>
        <RotateCcw className="size-4" aria-hidden="true" />
        Reactivar
      </Boton>
    );
  } else if (meta.estado === ESTADOS_META.EnProgreso) {
    accion = <p className="text-sm text-suave">{faltan === 1 ? 'Falta 1 cumplimiento' : `Faltan ${faltan} cumplimientos`}</p>;
  } else if (meta.estado === ESTADOS_META.Pendiente) {
    accion = <p className="text-sm text-suave">Aún sin cumplimientos · registra uno de sus hábitos para empezar</p>;
  }

  return (
    <article aria-labelledby={`meta-${meta.id}`} className="relative has-[[aria-expanded=true]]:z-20 flex flex-col gap-3 vidrio rounded-[1.25rem] p-4 sm:p-5">
      <div className="flex items-start justify-between gap-3">
        <h3 id={`meta-${meta.id}`} className="titulo text-lg font-semibold">
          {meta.nombre}
        </h3>
        <div className="flex shrink-0 items-center gap-1">
          <ChipEstado estado={meta.estado} />
          {alEliminar && (
            <MenuAcciones
              etiqueta={`Acciones de ${meta.nombre}`}
              acciones={[{ etiqueta: 'Eliminar', icono: Trash2, alElegir: () => alEliminar(meta), peligroso: true }]}
            />
          )}
        </div>
      </div>
      <div className="flex flex-col gap-1.5">
        <BarraAvance
          valor={meta.avance}
          maximo={meta.objetivoCumplimientos}
          etiqueta={`Avance de ${meta.nombre}`}
          color={PRESENTACION_ESTADO[meta.estado]?.barra}
        />
        <div className="flex flex-wrap justify-between gap-x-3 text-xs text-suave">
          <span className="numeros">
            <strong className="text-texto">{meta.avance}</strong> de {meta.objetivoCumplimientos} cumplimientos
          </span>
          <span>{textoPlazo(meta, hoy)}</span>
        </div>
      </div>
      {nombresHabitos.length > 0 && <p className="text-xs text-suave">Hábitos: {nombresHabitos.join(', ')}</p>}
      {accion && <div className="flex justify-end">{accion}</div>}
      {completar.isError && (
        <p role="alert" className="text-right text-xs font-medium text-peligro">
          {mensajeDeError(completar.error)}
        </p>
      )}
    </article>
  );
}
