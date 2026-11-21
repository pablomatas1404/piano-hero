// A PIE / PRIMERA PERSONA (19/11, pedido de Pablo): desde el autito, boton "Primera persona" -> aparece un cuadradito (el muniequito provisorio)
// al lado del auto y se camina. Las paredes de la casa frenan (y se entra por la puerta); el piso se sigue con un rayo hacia abajo (como el auto).
// JOYSTICK (lo principal, Pablo no usa teclado): stick izquierdo adelante/atras camina y a los costados gira el cuerpo (como el autito);
// stick derecho mueve la vista (como en el avion), apretarlo la centra; boton Y cambia la vista (ojos / desde atras).
// Teclado (por las dudas): W/S o flecha arriba/abajo = caminar; A/D = de costado; flechas izq/der = girar; boton derecho del mouse apretado = mirar;
// Shift = correr; V = vista (ojos / desde atras). Joystick: stick izquierdo camina, stick derecho mira.
// La camara es la principal del juego (MapaAuto deja de mover el auto y su camara mientras se camina).
using UnityEngine;
using UnityEngine.InputSystem;
using CesiumForUnity;

public class APie : MonoBehaviour
{
    public static bool activo;
    static APie inst;
    GameObject cuerpo; MeshRenderer rCuerpo; float yaw, pitch, mirarYaw; bool tercera; float nearAntes = -1f; float sueloY; bool sueloOk;   // yaw = cuerpo; mirarYaw = vista extra del stick derecho
    const float ALTO = 1.75f, OJOS = 1.65f, RADIO = 0.25f, VEL = 1.8f, VEL_CORRER = 4.5f;
    bool dosLados;
    public const int CAPA_PAREDES = 30;   // paredes de la casa (solo tienen colisionador mientras se camina, asi no molestan al auto ni al avion)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { if (FindFirstObjectByType<CesiumGeoreference>() == null) return; new GameObject("APie").AddComponent<APie>(); }
    void Awake() { inst = this; }

    public static void Alternar() { if (inst == null) return; if (activo) inst.Salir(); else inst.Entrar(); }

    void Entrar()
    {
        var auto = MapaAuto.AutoT; if (auto == null || !AvionDeCarton.enAuto) { Mapeo.Cartel("Primero bajate del avion al autito", 2.5f); return; }
        if (Casa.decorando) Casa.SalirDecorar();
        var geo = FindFirstObjectByType<CesiumGeoreference>();
        if (cuerpo == null)
        {
            cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cube); cuerpo.name = "Muniequito"; Destroy(cuerpo.GetComponent<Collider>());
            cuerpo.transform.localScale = new Vector3(0.45f, ALTO, 0.3f);
            rCuerpo = cuerpo.GetComponent<MeshRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh); var col = new Color(0.95f, 0.55f, 0.1f); m.SetColor("_BaseColor", col); m.SetColor("_Color", col); rCuerpo.sharedMaterial = m;
            // naricita para saber para donde mira
            var nariz = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(nariz.GetComponent<Collider>()); nariz.transform.SetParent(cuerpo.transform, false);
            nariz.transform.localScale = new Vector3(0.4f, 0.08f, 0.6f); nariz.transform.localPosition = new Vector3(0f, 0.38f, 0.6f); nariz.GetComponent<MeshRenderer>().sharedMaterial = m;
        }
        cuerpo.transform.SetParent(geo != null ? geo.transform : null, true);
        Vector3 f = auto.forward; f.y = 0f; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
        Vector3 r = Vector3.Cross(Vector3.up, f);
        Vector3 p = auto.position + r * 2.5f;
        yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg; pitch = 0f; mirarYaw = 0f; sueloOk = false;
        cuerpo.transform.position = p; cuerpo.SetActive(true);
        Casa.Colisiones(true); dosLados = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
        var cam = Camera.main; if (cam != null) { nearAntes = cam.nearClipPlane; cam.nearClipPlane = 0.08f; }
        activo = true;
        Mapeo.Cartel("A PIE: stick izquierdo camina y gira, stick derecho mira (apretalo para centrar), boton Y cambia la vista", 5f);
    }

    void Salir()
    {
        activo = false; if (cuerpo != null) cuerpo.SetActive(false);
        Casa.Colisiones(false); Physics.queriesHitBackfaces = dosLados;
        var cam = Camera.main; if (cam != null && nearAntes > 0f) cam.nearClipPlane = nearAntes;
    }

    void Update()
    {
        if (!activo) return;
        if (!AvionDeCarton.enAuto || cuerpo == null) { Salir(); return; }
        float dt = Time.deltaTime; if (dt <= 0f) return;
        var kb = Keyboard.current; var gp = Gamepad.current; var mouse = Mouse.current;
        bool escribe = Controles.escribiendo;
        float adel = 0f, lado = 0f, giro = 0f; bool correr = false;
        if (kb != null && !escribe)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) adel += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) adel -= 1f;
            if (kb.dKey.isPressed) lado += 1f; if (kb.aKey.isPressed) lado -= 1f;
            if (kb.rightArrowKey.isPressed || kb.eKey.isPressed) giro += 1f; if (kb.leftArrowKey.isPressed || kb.qKey.isPressed) giro -= 1f;
            correr = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            if (kb.vKey.wasPressedThisFrame) tercera = !tercera;
        }
        if (gp != null && !escribe)
        {
            Vector2 ls = gp.leftStick.ReadValue(), rs = gp.rightStick.ReadValue();
            if (Mathf.Abs(ls.y) > 0.2f) adel += ls.y;
            if (Mathf.Abs(ls.x) > 0.2f) giro += ls.x;   // como el volante del autito
            if (rs.magnitude > 0.2f) { mirarYaw = Mathf.Clamp(mirarYaw + rs.x * 110f * dt, -170f, 170f); pitch = Mathf.Clamp(pitch + rs.y * 80f * dt, -75f, 75f); }
            if (gp.rightStickButton.wasPressedThisFrame) { mirarYaw = 0f; pitch = 0f; }
            if (gp.buttonNorth.wasPressedThisFrame) tercera = !tercera;
            correr |= gp.leftStickButton.isPressed || ls.y > 0.95f;
        }
        yaw += giro * 100f * dt;
        if (mouse != null && mouse.rightButton.isPressed && !Tablet.Sobre(new Vector2(mouse.position.ReadValue().x, Screen.height - mouse.position.ReadValue().y)))
        {
            Vector2 d = mouse.delta.ReadValue(); yaw += d.x * 0.15f; pitch = Mathf.Clamp(pitch + d.y * 0.15f, -75f, 75f);
        }
        if ((Mathf.Abs(adel) > 0.1f || Mathf.Abs(lado) > 0.1f) && Mathf.Abs(mirarYaw) > 0.01f) { yaw += mirarYaw; mirarYaw = 0f; }   // caminar hacia donde se mira
        // movimiento horizontal con las paredes de la casa frenando (resbala a lo largo de la pared)
        Quaternion q = Quaternion.Euler(0f, yaw, 0f);
        Vector3 mov = q * new Vector3(Mathf.Clamp(lado, -1f, 1f), 0f, Mathf.Clamp(adel, -1f, 1f));
        if (mov.sqrMagnitude > 1f) mov.Normalize();
        mov *= (correr ? VEL_CORRER : VEL) * dt;
        var t = cuerpo.transform; Vector3 pos = t.position;
        for (int k = 0; k < 2 && mov.sqrMagnitude > 1e-8f; k++)
        {
            RaycastHit h; Vector3 pies = pos - Vector3.up * (ALTO * 0.5f); Vector3 p1 = pies + Vector3.up * (RADIO + 0.5f), p2 = pies + Vector3.up * (ALTO - RADIO - 0.05f);
            if (Physics.CapsuleCast(p1, p2, RADIO, mov.normalized, out h, mov.magnitude + 0.02f, 1 << CAPA_PAREDES, QueryTriggerInteraction.Ignore))
            {
                Vector3 n = h.normal; n.y = 0f; if (n.sqrMagnitude > 1e-4f) n.Normalize();
                pos += mov.normalized * Mathf.Max(0f, h.distance - 0.02f);
                mov -= mov.normalized * h.distance; mov -= Vector3.Dot(mov, n) * n;
            }
            else { pos += mov; mov = Vector3.zero; }
        }
        // pegado al piso (piso de la casa o terreno de Cesium); suavizado y sin saltar a los techos de las paredes
        RaycastHit hp;
        if (Physics.Raycast(pos + Vector3.up * 1.2f, Vector3.down, out hp, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!sueloOk) { sueloY = hp.point.y; sueloOk = true; }
            sueloY = Mathf.MoveTowards(sueloY, hp.point.y, 3f * dt + 0.02f);
        }
        else if (!sueloOk) { sueloY = pos.y - ALTO * 0.5f; sueloOk = true; }
        pos.y = sueloY + ALTO * 0.5f;
        t.position = pos; t.rotation = q;
    }

    void LateUpdate()
    {
        if (!activo || cuerpo == null) return;
        var cam = Camera.main; if (cam == null) return;
        var t = cuerpo.transform; Quaternion mira = Quaternion.Euler(-pitch, yaw + mirarYaw, 0f);
        if (!tercera) { cam.transform.position = t.position + Vector3.up * (OJOS - ALTO * 0.5f); cam.transform.rotation = mira; rCuerpo.enabled = false; foreach (var r in cuerpo.GetComponentsInChildren<MeshRenderer>()) r.enabled = false; }
        else
        {
            foreach (var r in cuerpo.GetComponentsInChildren<MeshRenderer>()) r.enabled = true;
            Vector3 centro = t.position + Vector3.up * 0.6f; Vector3 atras = mira * new Vector3(0f, 0.8f, -3.2f);
            RaycastHit h; float dist = atras.magnitude;
            if (Physics.SphereCast(centro, 0.2f, atras.normalized, out h, dist, 1 << CAPA_PAREDES, QueryTriggerInteraction.Ignore)) dist = Mathf.Max(0.4f, h.distance - 0.1f);   // la pared no tapa la camara
            cam.transform.position = centro + atras.normalized * dist; cam.transform.rotation = Quaternion.LookRotation(centro - cam.transform.position, Vector3.up);
        }
    }

    // botones grandes abajo (el de arriba quedaba tapado por "Instrucciones")
    void OnGUI()
    {
        if (!AvionDeCarton.enAuto || Casa.decorando) return;
        var s = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };
        float W = Screen.width, H = Screen.height;
        var bg = GUI.backgroundColor;
        if (!activo)
        {
            GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
            if (GUI.Button(new Rect(W * 0.5f - 390f, H - 90f, 210f, 50f), "BAJAR A CAMINAR", s)) Entrar();
        }
        else
        {
            if (GUI.Button(new Rect(W * 0.5f - 390f, H - 90f, 210f, 50f), tercera ? "VER CON LOS OJOS" : "VER DESDE ATRAS", s)) tercera = !tercera;
            GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
            if (GUI.Button(new Rect(W * 0.5f + 180f, H - 90f, 210f, 50f), "VOLVER AL AUTO", s)) Salir();
            Vector3 puerta, afuera;
            if (cuerpo != null && Casa.Puerta(out puerta, out afuera))
            {
                Vector3 d = cuerpo.transform.position - puerta; d.y = 0f;
                if (d.magnitude < 2.2f)
                {
                    bool estaAfuera = Vector3.Dot(d, afuera) > 0f;
                    GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
                    if (GUI.Button(new Rect(W * 0.5f - 105f, H - 150f, 210f, 50f), estaAfuera ? "ENTRAR" : "SALIR", s))
                    {
                        Vector3 p = puerta + afuera * (estaAfuera ? -1.2f : 1.2f); p.y = cuerpo.transform.position.y;
                        cuerpo.transform.position = p; yaw = Mathf.Atan2(-afuera.x * (estaAfuera ? 1f : -1f), -afuera.z * (estaAfuera ? 1f : -1f)) * Mathf.Rad2Deg; mirarYaw = 0f; sueloOk = false;
                    }
                }
            }
        }
        GUI.backgroundColor = bg;
    }
}
