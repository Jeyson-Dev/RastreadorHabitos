import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { CloudOff, CloudRain, CloudSun, MapPin } from 'lucide-react';
import { CLAVES_HABITOS, consultarPlanDeHoy } from '../../api/habitos';
import { mensajeDeError } from '../../api/cliente';
import Tarjeta from '../../componentes/Tarjeta';

const CLAVE_CIUDAD = 'rh.ciudad';
const CIUDAD_POR_DEFECTO = 'Santo Domingo';
const MAX_CIUDAD = 100;

function leerCiudad() {
  try {
    return localStorage.getItem(CLAVE_CIUDAD)?.trim() || CIUDAD_POR_DEFECTO;
  } catch {
    return CIUDAD_POR_DEFECTO;
  }
}

// Clima del plan de hoy (WeatherAPI). Va aparte de los hábitos: si el servicio no está disponible
// (503, por ejemplo sin clave configurada), solo esta tarjeta lo indica y el resto sigue funcionando.
export default function TarjetaClima() {
  const [ciudad, setCiudad] = useState(leerCiudad);
  const [editando, setEditando] = useState(false);
  const [borrador, setBorrador] = useState(ciudad);
  const [errorCiudad, setErrorCiudad] = useState(null);

  const plan = useQuery({
    queryKey: CLAVES_HABITOS.planDeHoy(ciudad),
    queryFn: ({ signal }) => consultarPlanDeHoy(ciudad, signal),
    retry: false,
    staleTime: 30 * 60 * 1000,
    refetchOnWindowFocus: false,
  });

  function guardarCiudad(evento) {
    evento.preventDefault();
    const nueva = borrador.trim();
    if (!nueva || nueva.length > MAX_CIUDAD) {
      setErrorCiudad(`Escribe una ciudad de hasta ${MAX_CIUDAD} caracteres.`);
      return;
    }
    try {
      localStorage.setItem(CLAVE_CIUDAD, nueva);
    } catch {
      // sin almacenamiento: vale solo para esta visita
    }
    setErrorCiudad(null);
    setCiudad(nueva);
    setEditando(false);
  }

  let cuerpo;
  if (plan.isPending) {
    cuerpo = <div className="h-16 animate-pulse rounded-lg bg-superficie-suave" aria-label="Cargando el clima" />;
  } else if (plan.isError) {
    const sinServicio = plan.error?.estado === 503 || plan.error?.estado === 0;
    cuerpo = (
      <div className="flex gap-3 text-sm">
        <CloudOff className="mt-0.5 size-5 shrink-0 text-suave" aria-hidden="true" />
        <div className="flex flex-col gap-0.5">
          <p className="font-medium">{sinServicio ? 'El clima no está disponible ahora' : 'No pudimos consultar el clima'}</p>
          <p className="text-suave">{mensajeDeError(plan.error)}</p>
        </div>
      </div>
    );
  } else {
    const { pronostico, favorableAlAireLibre, recomendacion } = plan.data;
    const Icono = pronostico.probabilidadLluvia >= 50 ? CloudRain : CloudSun;
    cuerpo = (
      <div className="flex flex-col gap-2">
        <div className="flex items-center gap-3">
          <Icono className="size-8 text-info" aria-hidden="true" />
          <div className="flex flex-col">
            <p className="numeros text-lg font-semibold">
              {Math.round(pronostico.temperaturaMaximaC)}° <span className="text-sm font-normal text-suave">/ {Math.round(pronostico.temperaturaMinimaC)}°</span>
            </p>
            <p className="text-xs text-suave">
              {pronostico.condicion} · lluvia {pronostico.probabilidadLluvia}%
            </p>
          </div>
        </div>
        <p className={`text-xs font-medium ${favorableAlAireLibre ? 'text-acento' : 'text-aviso'}`}>
          {favorableAlAireLibre ? 'Buen día para el aire libre' : 'Mejor bajo techo'}
        </p>
        {recomendacion && <p className="text-xs text-suave">{recomendacion}</p>}
      </div>
    );
  }

  return (
    <Tarjeta aria-labelledby="titulo-clima" className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-2">
        <h3 id="titulo-clima" className="inline-flex items-center gap-1.5 text-sm font-medium">
          <MapPin className="size-4 text-suave" aria-hidden="true" />
          <span className="truncate">{plan.data?.ciudad ? `${plan.data.ciudad}${plan.data.pais ? `, ${plan.data.pais}` : ''}` : ciudad}</span>
        </h3>
        {!editando && (
          <button
            type="button"
            onClick={() => {
              setBorrador(ciudad);
              setEditando(true);
            }}
            className="shrink-0 text-xs font-medium text-acento hover:underline"
          >
            Cambiar ciudad
          </button>
        )}
      </div>
      {editando ? (
        <form onSubmit={guardarCiudad} noValidate className="flex flex-col gap-2">
          <label htmlFor="ciudad-clima" className="sr-only">
            Ciudad
          </label>
          <div className="flex gap-2">
            <input
              id="ciudad-clima"
              value={borrador}
              onChange={(e) => setBorrador(e.target.value)}
              maxLength={MAX_CIUDAD}
              aria-invalid={errorCiudad ? true : undefined}
              className="h-9 min-w-0 flex-1 rounded-lg border border-borde bg-superficie px-3 text-sm focus:border-acento focus:ring-2 focus:ring-acento/30 focus:outline-none"
              autoFocus
            />
            <button type="submit" className="h-9 rounded-lg bg-acento px-3 text-sm font-medium text-sobre-acento hover:bg-acento-hover">
              Guardar
            </button>
          </div>
          {errorCiudad && <p className="text-xs font-medium text-peligro">{errorCiudad}</p>}
        </form>
      ) : (
        cuerpo
      )}
    </Tarjeta>
  );
}
