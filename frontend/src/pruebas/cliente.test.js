import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { configurarCliente, ErrorApi, MENSAJE_SIN_CONEXION, mensajeDeError, pedir } from '../api/cliente';

function respuestaJson(estado, cuerpo) {
  return new Response(cuerpo === undefined ? null : JSON.stringify(cuerpo), {
    status: estado,
    headers: cuerpo === undefined ? {} : { 'Content-Type': 'application/json; charset=utf-8' },
  });
}

describe('cliente de la API', () => {
  let fetchFalso;
  let sesionInvalida;
  let token;

  beforeEach(() => {
    fetchFalso = vi.fn();
    vi.stubGlobal('fetch', fetchFalso);
    sesionInvalida = vi.fn();
    token = null;
    configurarCliente({ token: () => token, sesionInvalida });
  });

  afterEach(() => vi.unstubAllGlobals());

  it('envía el token como Authorization: Bearer cuando hay sesión', async () => {
    // Preparar
    token = 'abc.def.ghi';
    fetchFalso.mockResolvedValue(respuestaJson(200, { ok: true }));

    // Actuar
    await pedir('/api/habitos');

    // Verificar
    expect(fetchFalso.mock.calls[0][1].headers.Authorization).toBe('Bearer abc.def.ghi');
  });

  it('no envía Authorization sin sesión y serializa el cuerpo como JSON', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuestaJson(200, { token: 't' }));

    // Actuar
    await pedir('/api/sesion/iniciar', { metodo: 'POST', cuerpo: { email: 'a@b.com' } });

    // Verificar
    const [, opciones] = fetchFalso.mock.calls[0];
    expect(opciones.headers.Authorization).toBeUndefined();
    expect(opciones.headers['Content-Type']).toBe('application/json');
    expect(opciones.body).toBe('{"email":"a@b.com"}');
  });

  it('omite los parámetros de consulta vacíos', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuestaJson(200, []));

    // Actuar
    await pedir('/api/auditoria', { consulta: { entidad: 'Usuario', desde: '', hasta: undefined } });

    // Verificar
    expect(fetchFalso.mock.calls[0][0]).toBe('/api/auditoria?entidad=Usuario');
  });

  it('usa el mensaje { error } del servidor tal cual', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuestaJson(409, { error: 'Hoy no es un día programado para este hábito.' }));

    // Actuar
    const error = await pedir('/api/habitos/1/registros', { metodo: 'POST', cuerpo: {} }).catch((e) => e);

    // Verificar
    expect(error).toBeInstanceOf(ErrorApi);
    expect(error.estado).toBe(409);
    expect(error.message).toBe('Hoy no es un día programado para este hábito.');
  });

  it.each([
    ['sin cuerpo', undefined],
    ['con error vacío', { error: '' }],
    ['con error en blanco', { error: '   ' }],
  ])('usa un mensaje por código cuando la respuesta llega %s', async (_caso, cuerpo) => {
    // Preparar
    fetchFalso.mockResolvedValue(respuestaJson(403, cuerpo));

    // Actuar
    const error = await pedir('/api/usuarios').catch((e) => e);

    // Verificar
    expect(error.message).toBe('No tienes permiso para hacer esto.');
  });

  it('ante un 500 nunca muestra lo que mandó el servidor', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuestaJson(500, { error: 'System.NullReferenceException at ...' }));

    // Actuar
    const error = await pedir('/api/metas').catch((e) => e);

    // Verificar
    expect(error.estado).toBe(500);
    expect(error.message).not.toMatch(/Exception/);
  });

  it('sin conexión responde un mensaje comprensible con estado 0', async () => {
    // Preparar
    fetchFalso.mockRejectedValue(new TypeError('Failed to fetch'));

    // Actuar
    const error = await pedir('/api/metas').catch((e) => e);

    // Verificar
    expect(error.estado).toBe(0);
    expect(error.message).toBe(MENSAJE_SIN_CONEXION);
  });

  it('un 401 con sesión avisa que la sesión ya no sirve', async () => {
    // Preparar
    token = 'vencido';
    fetchFalso.mockResolvedValue(respuestaJson(401, { error: 'Se requiere una sesión válida.' }));

    // Actuar
    await pedir('/api/habitos').catch(() => {});

    // Verificar
    expect(sesionInvalida).toHaveBeenCalledWith('Se requiere una sesión válida.', 'vencido');
  });

  it('un 401 tardío informa el token con el que salió, no el actual', async () => {
    // Preparar: la petición sale con el token A y, antes de que responda, ya hay otra sesión (B).
    token = 'token-A';
    let responder;
    fetchFalso.mockReturnValue(new Promise((resolver) => (responder = resolver)));
    const peticion = pedir('/api/habitos').catch(() => {});
    token = 'token-B';

    // Actuar
    responder(respuestaJson(401, { error: 'Se requiere una sesión válida.' }));
    await peticion;

    // Verificar
    expect(sesionInvalida).toHaveBeenCalledWith('Se requiere una sesión válida.', 'token-A');
  });

  it('un 401 sin sesión (credenciales incorrectas) no cierra nada', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuestaJson(401, { error: 'Correo o contraseña incorrectos.' }));

    // Actuar
    await pedir('/api/sesion/iniciar', { metodo: 'POST', cuerpo: {} }).catch(() => {});

    // Verificar
    expect(sesionInvalida).not.toHaveBeenCalled();
  });

  it('mensajeDeError da un texto genérico para errores que no son de la API', () => {
    expect(mensajeDeError(new Error('detalle interno'))).not.toMatch(/detalle interno/);
  });
});
