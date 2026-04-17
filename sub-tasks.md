### Sub-task: Modellare ciclo di vita ordine
- **Description:** Creare il ciclo di vita per gli ordini nel sistema (creato, confermato, in lavorazione, pronto, consegnato, annullato).
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

### Sub-task: Creazione ordini da cassa/cassa volante
- **Description:** Implementare la funzionalità per la creazione di ordini direttamente da cassa o cassa volante.
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

### Sub-task: Preordine webapp
- **Description:** Sviluppare la funzionalità di preordine attraverso la webapp; includere codice alfanumerico/QR, validazione e creazione.
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

### Sub-task: Gestione menu, varianti, allergeni, disponibilità piatti
- **Description:** Creare un sistema per gestire il menu, comprese varianti, allergeni e disponibilità dei piatti.
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

### Sub-task: Gestione pagamenti, movimenti cassa
- **Description:** Implementare la gestione dei pagamenti e dei movimenti di cassa.
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

### Sub-task: Stampa cucina
- **Description:** Sviluppare un sistema di stampa per la cucina tramite ESC/POS e monitor.
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

### Sub-task: Regola tavoli condizionale
- **Description:** Creare una regola condizionale per la gestione dei tavoli.
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

### Sub-task: Storico ordini
- **Description:** Implementare un sistema per visualizzare lo storico degli ordini.
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

### Sub-task: Report/export PDF, CSV, Excel
- **Description:** Sviluppare un sistema per generare report e esportare i dati in vari formati (PDF, CSV, Excel).
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

### Sub-task: Supporto LAN/offline
- **Description:** Creare una funzionalità di supporto per funzionare in modalità LAN/offline.
- **Relationship to Epic:** Related to Epic #6
- **Labels:** enhancement

---

## Epic: Ottimizzazione interfacce e layout responsive

### Piano di implementazione (dettagliato)

1. **Audit UI e baseline responsive**
   - Mappare i flussi attuali: NowCalling (pubblico), Receptionist, HeadWaiter, NavMenu.
   - Identificare breakpoint e criticità su laptop/tablet/mobile (overflow tabelle, CTA troppo piccole, gerarchia visiva).
   - Definire baseline tecnica: mobile-first, breakpoint unificati, componenti condivisi.

2. **Refactor responsive flussi pubblici/servizio**
   - Uniformare spaziature, densità contenuti e tipografia tra pagine pubbliche e operative.
   - Applicare pattern coerenti per card, filtri, azioni primarie/secondarie e tabelle responsivi.
   - Introdurre fallback leggibili su small screen (stack verticale, scroll orizzontale controllato, priorità azioni).

3. **Layout dedicato Cassiere (tablet/laptop)**
   - Ottimizzare layout a due colonne (azioni + riepilogo) con focus su velocità operativa.
   - Rendere visibili i KPI principali senza scroll e mantenere CTA principali sempre raggiungibili.
   - Validare usabilità con orientamento landscape tablet e laptop 1366x768+.

4. **Layout dedicato Cassa volante (mobile)**
   - Definire esperienza one-hand: pulsanti grandi, sequenza guidata, riduzione campi visibili.
   - Gestire form/order flow in step compatti e minimizzare interazioni non essenziali.
   - Validare su viewport 360x800 / 390x844 / 412x915.

5. **Verifica dispositivi target e hardening UX**
   - Eseguire smoke test manuali per tutti i flussi critici (chiamate, menu, pagamenti online, servizio sala).
   - Allineare microcopy, feedback visuali, loading/error states tra pubblico e servizio.
   - Consolidare checklist QA responsive e criteri di accettazione per regressioni future.

### Sotto-issue da collegare all'epic

### Sub-task: Responsive per chiamate, menu, pagamenti online
- **Description:** Adattare i flussi di chiamata pubblica, menu e pagamenti online a viewport laptop/tablet/mobile con comportamento coerente e accessibile.
- **Relationship to Epic:** Sub-issue of Epic "Ottimizzazione interfacce e layout responsive"
- **Labels:** enhancement, ui/ux, responsive

---

### Sub-task: Layout dedicato cassiere (tablet/laptop)
- **Description:** Progettare e implementare layout operativo ottimizzato per ruolo cassiere su tablet/laptop, con priorità su rapidità e leggibilità.
- **Relationship to Epic:** Sub-issue of Epic "Ottimizzazione interfacce e layout responsive"
- **Labels:** enhancement, ui/ux, responsive, role:cassiere

---

### Sub-task: Layout dedicato cassa volante (mobile)
- **Description:** Progettare e implementare layout mobile-first per cassa volante, con interazioni rapide e minimizzazione errori input.
- **Relationship to Epic:** Sub-issue of Epic "Ottimizzazione interfacce e layout responsive"
- **Labels:** enhancement, ui/ux, responsive, mobile

---

### Sub-task: Verifica UI dispositivi target
- **Description:** Definire e completare una matrice di test UI su risoluzioni/dispositivi target, includendo regressioni su componenti chiave.
- **Relationship to Epic:** Sub-issue of Epic "Ottimizzazione interfacce e layout responsive"
- **Labels:** enhancement, qa, responsive

---

### Sub-task: Allineamento UX tra flussi pubblici/servizio
- **Description:** Uniformare pattern UX/UI, microcopy, feedback e stati tra flussi pubblici e flussi di servizio per coerenza end-to-end.
- **Relationship to Epic:** Sub-issue of Epic "Ottimizzazione interfacce e layout responsive"
- **Labels:** enhancement, ui/ux
