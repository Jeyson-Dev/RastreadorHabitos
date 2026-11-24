import { useState } from 'react';
import { Link } from 'react-router';
import { mensajeDeError } from '../../api/cliente';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Campo from '../../componentes/Campo';
import { useSesion } from '../../sesion/contextoSesion';

// Inicio de sesión (versión base; el bloque de Acceso define su diseño final y el resto de flujos).
export default function IniciarSesion() {
  const { iniciarSesion, aviso, limpiarAviso } = useSesion();
  const [email, setEmail] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState(null);
  const [enviando, setEnviando] = useState(false);

  async function alEnviar(evento) {
    evento.preventDefault();
    setError(null);
    limpiarAviso();
    if (!email.trim() || !contrasena) {
      setError('Escribe tu correo y tu contraseña.');
      return;
    }
    setEnviando(true);
    try {
      await iniciarSesion(email.trim(), contrasena);
    } catch (e) {
      setError(mensajeDeError(e));
      setEnviando(false);
    }
  }

  return (
    <main className="flex min-h-dvh items-center justify-center p-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 flex flex-col items-center gap-3 text-center">
          <img src="/icono.svg" alt="" className="size-10" />
          <h1 className="text-xl font-semibold">Inicia sesión</h1>
          <p className="text-sm text-suave">Registra tus hábitos y avanza en tus metas.</p>
        </div>

        <form onSubmit={alEnviar} noValidate className="flex flex-col gap-4 rounded-xl border border-borde bg-superficie p-6">
          {aviso && <Aviso tipo="aviso">{aviso}</Aviso>}
          {error && <Aviso tipo="error">{error}</Aviso>}
          <Campo
            etiqueta="Correo"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <Campo
            etiqueta="Contraseña"
            type="password"
            autoComplete="current-password"
            value={contrasena}
            onChange={(e) => setContrasena(e.target.value)}
            required
          />
          <Boton type="submit" cargando={enviando} className="w-full">
            Iniciar sesión
          </Boton>
        </form>

        <p className="mt-6 text-center text-sm text-suave">
          ¿No tienes cuenta?{' '}
          <Link to="/registro" className="font-medium text-acento hover:underline">
            Regístrate
          </Link>
        </p>
      </div>
    </main>
  );
}
