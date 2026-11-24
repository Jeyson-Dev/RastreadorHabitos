import { useId, useRef, useState } from 'react';
import { Link } from 'react-router';
import { mensajeDeError } from '../../api/cliente';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Campo from '../../componentes/Campo';
import { describirDias, diaLocalDe } from '../../utilidades/fechas';
import { enfocarPrimerInvalido } from '../../utilidades/formularios';
import { useCrearMeta } from './consultas';

const MAX_NOMBRE = 150;

// Nueva meta: nombre, cuántos cumplimientos, hasta cuándo y qué hábitos cuentan. Mismas reglas que
// el servidor: fecha límite no anterior a hoy y al menos un hábito.
export default function FormularioMeta({ habitos, alCrear }) {
  const crear = useCrearMeta();
  const hoy = diaLocalDe(new Date());
  const [nombre, setNombre] = useState('');
  const [objetivo, setObjetivo] = useState('');
  const [fechaLimite, setFechaLimite] = useState('');
  const [habitoIds, setHabitoIds] = useState([]);
  const [errores, setErrores] = useState({});
  const formulario = useRef(null);
  const idErrorHabitos = useId();

  const alternarHabito = (id) => setHabitoIds((actuales) => (actuales.includes(id) ? actuales.filter((h) => h !== id) : [...actuales, id]));

  function alEnviar(evento) {
    evento.preventDefault();
    if (crear.isPending) return;
    const encontrados = {};
    const objetivoNumero = Number(objetivo);
    if (!nombre.trim()) encontrados.nombre = 'Escribe un nombre para la meta.';
    else if (nombre.trim().length > MAX_NOMBRE) encontrados.nombre = `El nombre puede tener hasta ${MAX_NOMBRE} caracteres.`;
    if (!objetivo.trim() || !Number.isInteger(objetivoNumero) || objetivoNumero < 1) encontrados.objetivo = 'Indica un número entero de 1 o más.';
    if (!fechaLimite) encontrados.fechaLimite = 'Elige la fecha límite.';
    else if (fechaLimite < hoy) encontrados.fechaLimite = 'La fecha límite no puede ser anterior a hoy.';
    if (habitoIds.length === 0) encontrados.habitos = 'Elige al menos un hábito.';
    setErrores(encontrados);
    if (Object.keys(encontrados).length > 0) {
      enfocarPrimerInvalido(formulario.current);
      return;
    }
    crear.mutate(
      { nombre: nombre.trim(), objetivoCumplimientos: objetivoNumero, fechaLimite, habitoIds },
      { onSuccess: (meta) => alCrear?.(meta) },
    );
  }

  if (habitos.length === 0) {
    return (
      <div className="flex flex-col gap-3 text-sm">
        <p>Una meta cuenta los cumplimientos de tus hábitos, así que primero necesitas al menos uno.</p>
        <Link to="/habitos?nuevo=1" className="font-medium text-acento hover:underline">
          Crear un hábito
        </Link>
      </div>
    );
  }

  return (
    <form ref={formulario} onSubmit={alEnviar} noValidate className="flex flex-col gap-5">
      {crear.isError && <Aviso tipo="error">{mensajeDeError(crear.error)}</Aviso>}
      <Campo
        etiqueta="Nombre"
        placeholder="Leer 20 veces este mes"
        value={nombre}
        onChange={(e) => setNombre(e.target.value)}
        error={errores.nombre}
        maxLength={MAX_NOMBRE}
        data-autofocus
      />
      <div className="grid gap-4 sm:grid-cols-2">
        <Campo
          etiqueta="Cumplimientos objetivo"
          type="number"
          inputMode="numeric"
          min={1}
          step={1}
          placeholder="20"
          value={objetivo}
          onChange={(e) => setObjetivo(e.target.value)}
          error={errores.objetivo}
        />
        <Campo
          etiqueta="Fecha límite"
          type="date"
          min={hoy}
          value={fechaLimite}
          onChange={(e) => setFechaLimite(e.target.value)}
          error={errores.fechaLimite}
        />
      </div>
      <fieldset aria-describedby={errores.habitos ? idErrorHabitos : undefined} className="flex flex-col gap-2">
        <legend className="mb-1.5 text-sm font-medium">Hábitos que cuentan</legend>
        <ul className="flex flex-col divide-y divide-borde rounded-lg border border-borde">
          {habitos.map((habito) => (
            <li key={habito.id}>
              <label className="flex cursor-pointer items-center gap-3 px-3 py-2.5 hover:bg-superficie-suave">
                <input
                  type="checkbox"
                  checked={habitoIds.includes(habito.id)}
                  onChange={() => alternarHabito(habito.id)}
                  aria-invalid={errores.habitos ? true : undefined}
                  className="size-4 accent-[var(--acento)]"
                />
                <span className="flex min-w-0 flex-col">
                  <span className="truncate text-sm font-medium">{habito.nombre}</span>
                  <span className="text-xs text-suave">{describirDias(habito.dias)}</span>
                </span>
              </label>
            </li>
          ))}
        </ul>
        {errores.habitos && (
          <p id={idErrorHabitos} className="text-xs font-medium text-peligro">
            {errores.habitos}
          </p>
        )}
      </fieldset>
      <p className="text-xs text-suave">
        El avance cuenta los días cumplidos de estos hábitos desde hoy hasta la fecha límite. La meta empieza Pendiente y pasa a En
        progreso con el primer cumplimiento.
      </p>
      <Boton type="submit" cargando={crear.isPending} tamano="lg" className="w-full">
        Crear meta
      </Boton>
    </form>
  );
}
