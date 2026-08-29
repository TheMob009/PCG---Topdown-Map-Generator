using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MissionGenerator : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // CONFIGURACIÓN DE LA GENERACIÓN
    // -------------------------------------------------------------------------

    [Header("Generation")]

    [Tooltip(
        "Semilla utilizada para seleccionar las reglas. " +
        "La misma configuración y la misma seed deben producir " +
        "el mismo resultado."
    )]
    [SerializeField]
    private int seed = 12345;


    [Tooltip(
        "Cantidad de expansiones realizadas antes de finalizar la misión."
    )]
    [Range(1, 10)]
    [SerializeField]
    private int expansionSteps = 4;


    [Header("Grammar")]

    [SerializeField]
    private string startSymbol = "M";


    [SerializeField]
    private string startProduction = "STG";


    [SerializeField]
    private string taskSymbol = "T";


    [SerializeField]
    private List<string> taskProductions =
        new List<string>()
        {
            "CT",
            "ET",
            "RT",
            "KTL"
        };


    [SerializeField]
    private string terminalProduction = "C";


    // -------------------------------------------------------------------------
    // GRAMÁTICA SECUENCIAL PARA MISIONES
    // -------------------------------------------------------------------------
    //
    // En el primer ejercicio del laboratorio se implementó una gramática con
    // reescritura PARALELA.
    //
    // En este caso se utilizará una estrategia diferente.
    //
    // La generación de la misión será SECUENCIAL.
    //
    //
    // -------------------------------------------------------------------------
    // DIFERENCIA CON LA EXPANSIÓN PARALELA
    // -------------------------------------------------------------------------
    //
    // En una expansión paralela se procesan todos los símbolos de una cadena
    // antes de obtener la siguiente iteración.
    //
    // En la misión se seleccionará una tarea pendiente y se aplicará una
    // producción sobre ella.
    //
    // Por ejemplo:
    //
    //      M
    //
    // aplicando:
    //
    //      M -> STG
    //
    // se obtiene:
    //
    //      STG
    //
    // El símbolo T representa una tarea todavía pendiente de definir.
    //
    //
    // -------------------------------------------------------------------------
    // PRODUCCIONES DE TAREA
    // -------------------------------------------------------------------------
    //
    // Las producciones disponibles inicialmente son:
    //
    //      T -> CT
    //      T -> ET
    //      T -> RT
    //      T -> KTL
    //
    // Cada una introduce una acción concreta y conserva un símbolo T,
    // permitiendo que la misión continúe creciendo.
    //
    // Por ejemplo:
    //
    //      STG
    //
    // utilizando:
    //
    //      T -> KTL
    //
    // produce:
    //
    //      SKTLG
    //
    // La siguiente expansión podrá volver a actuar sobre la T restante.
    //
    //
    // -------------------------------------------------------------------------
    // SELECCIÓN DE PRODUCCIONES
    // -------------------------------------------------------------------------
    //
    // En cada expansión se seleccionará una de las producciones disponibles.
    //
    // Para que el resultado sea reproducible se utilizará System.Random junto
    // al parámetro seed.
    //
    // De esta manera:
    //
    //      misma seed + mismos parámetros
    //
    // debe producir la misma misión.
    //
    //
    // -------------------------------------------------------------------------
    // FINALIZACIÓN
    // -------------------------------------------------------------------------
    //
    // Después de ejecutar expansionSteps, la cadena todavía puede contener T.
    //
    // Para completar la misión se utilizará:
    //
    //      terminalProduction
    //
    // cuya configuración inicial corresponde a:
    //
    //      T -> C
    //
    // Al finalizar no deberían permanecer tareas pendientes representadas por T.
    //
    //
    // -------------------------------------------------------------------------
    // INTERPRETACIÓN
    // -------------------------------------------------------------------------
    //
    // Al igual que en el L-System, generar la cadena simbólica y darle
    // significado son procesos diferentes.
    //
    // Por ejemplo:
    //
    //      S C K L G
    //
    // puede interpretarse como:
    //
    //      S -> Comienza la misión.
    //      C -> Derrota a los enemigos.
    //      K -> Obtén una llave.
    //      L -> Abre una cerradura.
    //      G -> Completa el objetivo principal.
    //
    // La función GetDescription() que realiza esta traducción ya se encuentra
    // implementada.
    //
    //
    public void GenerateMission()
    {
        if (!ValidateGrammar())
        {
            return;
        }

        string current = startSymbol; //Current es el estado de la cadena.
        //en este momento current = M
        //Para reprensentar el inicio de la cadena es necesario aplicar startProduction
        //No se inicia por startProduction porque si se modifica la regla o el simbolo de partida cambia la grámatica
        List<string> derivation = new List<string>();
        derivation.Add(current);
        current = startProduction;
        derivation.Add(current);
        //En este momento current pasó de M -> STG aún no inicia la expansión secuencial T
        System.Random random = new System.Random(seed);
        //Se utiliza la semilla para replicar la misma secuencia de expansión

        int step = 0;
        while (step < expansionSteps)
        {

            //Es necesario conocer la posición de T dentro de la cadena
            //No se utiliza replace debido a que modificaría toda la cadena.
            //Hay que preguntar en que posición aparece por primera vez el simbolo
            //¿Donde está la T?
            int taskIndex = current.IndexOf(taskSymbol);
            //ahora mismo current es STG por lo que hay una T así que TaskIndex = 1
            //ahora hay que elegir las task production de forma aleatoria
            //{CT, ET, RT,  KTL} 
            //¿Que regla usar?
            int productionIndex = random.Next(0, taskProductions.Count);//se utiliza el taskProduction.Count como limite superior
                                                                        //para asegurarse que siempre se seleccione una de las 4 reglas.
            string production = taskProductions[productionIndex];//guarda el indice en una variable para saber que regla es
                                                                 //Se debe construir una cadena con esta formula: ANTES + PRODUCCIÓN + DESPUÉS
            /*
             * Ejemplo STG con index 1 y production KTL
             * 
                ANTES   T   DESPUÉS

                 S      T     G
                 S     KTL    G

            Resultado: SKTLG
             */

            //Se va a utilizar stringbuilder ya que permite crear y modificar texto
            //así se copia current para modificarlo
            StringBuilder currentCopy = new StringBuilder(current);
            //gracias a stringbuilder se puede modificar cualquier posición dentro de la cadena
            //tiene un método insert y un remove
            currentCopy.Remove(taskIndex, 1);//borrar el caracter T (siempre estará en la posición 1)
            currentCopy.Insert(taskIndex, production);//se inserta la regla en la posicion 1
            current = currentCopy.ToString();//se hace un cast para que currentCopy sea un string ya que es de tipo builder
            derivation.Add(current);


            step++;
        }
        


        // TODO: GENERACIÓN SECUENCIAL DE LA MISIÓN
        //
        // Implementar el proceso completo considerando:
        //
        // 1. Comenzar desde startSymbol y aplicar startProduction.
        // 2. Realizar expansionSteps expansiones secuenciales.
        // 3. Seleccionar las producciones de taskProductions utilizando seed.
        // 4. Reemplazar solamente una tarea pendiente en cada expansión.
        // 5. Finalizar los símbolos taskSymbol restantes con terminalProduction.
        // 6. Registrar la derivación de la gramática.
        // 7. Interpretar la cadena final utilizando GetDescription().
        // 8. Mostrar en Console:
        //
        //      - reglas
        //      - derivación
        //      - cadena final
        //      - misión interpretada
        //
        // El resultado debe ser reproducible utilizando la misma seed.
    }


    // -------------------------------------------------------------------------
    // INTERPRETACIÓN DE SÍMBOLOS
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // Cada símbolo terminal posee un significado dentro de la misión.
    //
    private string GetDescription(
        char symbol)
    {
        switch (symbol)
        {
            case 'S':
                return
                    "Comienza la misión.";

            case 'C':
                return
                    "Derrota a los enemigos.";

            case 'E':
                return
                    "Explora la zona.";

            case 'R':
                return
                    "Recolecta el recurso solicitado.";

            case 'K':
                return
                    "Obtén una llave.";

            case 'L':
                return
                    "Utiliza la llave para abrir una cerradura.";

            case 'G':
                return
                    "Completa el objetivo principal.";

            default:
                return null;
        }
    }


    // -------------------------------------------------------------------------
    // VALIDACIÓN
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // Su objetivo es detectar configuraciones básicas inválidas antes
    // de comenzar la generación.
    //
    private bool ValidateGrammar()
    {
        if (string.IsNullOrEmpty(
            startSymbol))
        {
            Debug.LogError(
                "Start Symbol no puede estar vacío.",
                this
            );

            return false;
        }


        if (string.IsNullOrEmpty(
            startProduction))
        {
            Debug.LogError(
                "Start Production no puede estar vacía.",
                this
            );

            return false;
        }


        if (string.IsNullOrEmpty(
            taskSymbol))
        {
            Debug.LogError(
                "Task Symbol no puede estar vacío.",
                this
            );

            return false;
        }


        if (taskProductions == null ||
            taskProductions.Count == 0)
        {
            Debug.LogError(
                "Debe existir al menos una producción para Task.",
                this
            );

            return false;
        }


        if (string.IsNullOrEmpty(
            terminalProduction))
        {
            Debug.LogError(
                "Terminal Production no puede estar vacía.",
                this
            );

            return false;
        }


        if (terminalProduction.Contains(
            taskSymbol))
        {
            Debug.LogError(
                "La producción terminal no debe volver a generar " +
                "el símbolo '" +
                taskSymbol +
                "'.",
                this
            );

            return false;
        }


        return true;
    }
}