using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class LSystemRule
{
    [Tooltip("Símbolo que será reemplazado durante la expansión.")]
    public string predecessor = "A";

    [Tooltip("Cadena que reemplazará al símbolo.")]
    public string successor = "AB";
}


public class ParallelGrammarGenerator : MonoBehaviour
{
    [Header("Parallel Grammar")]

    [SerializeField]
    private bool autoUpdate = true;


    [Tooltip("Cadena inicial de la gramática.")]
    [SerializeField]
    private string axiom = "A";


    [Tooltip(
        "Reglas de producción utilizadas durante la expansión.\n\n" +
        "Configuración base:\n" +
        "A -> AB\n" +
        "B -> A"
    )]
    [SerializeField]
    private List<LSystemRule> rules =
        new List<LSystemRule>()
        {
            new LSystemRule()
            {
                predecessor = "A",
                successor = "AB"
            },

            new LSystemRule()
            {
                predecessor = "B",
                successor = "A"
            }
        };


    [Tooltip("Cantidad de iteraciones de expansión.")]
    [Range(0, 10)]
    [SerializeField]
    private int iterations = 4;


    [SerializeField]
    private bool showDerivation = true;


    // -------------------------------------------------------------------------
    // GENERACIÓN DE LA EXPANSIÓN
    // -------------------------------------------------------------------------
    //
    // Una gramática define reglas que permiten reemplazar símbolos por nuevas
    // cadenas.
    //
    // Para la configuración inicial del laboratorio:
    //
    //      Axiom: A
    //
    //      A -> AB
    //      B -> A
    //
    // la expansión esperada es:
    //
    //      Iteración 0: A
    //      Iteración 1: AB
    //      Iteración 2: ABA
    //      Iteración 3: ABAAB
    //      Iteración 4: ABAABABA
    //
    //
    // -------------------------------------------------------------------------
    // REESCRITURA PARALELA
    // -------------------------------------------------------------------------
    //
    // La característica importante de esta primera implementación es que las
    // reglas se aplican de manera PARALELA.
    //
    // Esto significa que todos los símbolos de una iteración son evaluados
    // utilizando la misma cadena de origen.
    //
    // Por ejemplo:
    //
    //      cadena actual:
    //
    //          ABA
    //
    //      reglas:
    //
    //          A -> AB
    //          B -> A
    //
    // Durante esa iteración se evalúa:
    //
    //          A      B      A
    //          |      |      |
    //          AB     A      AB
    //
    // y solamente después de procesar toda la cadena se obtiene:
    //
    //          ABAAB
    //
    // No se debe utilizar el resultado parcial de una sustitución para decidir
    // las sustituciones restantes de la misma iteración.
    //
    //
    // -------------------------------------------------------------------------
    // SÍMBOLOS SIN REGLA
    // -------------------------------------------------------------------------
    //
    // No todos los símbolos necesitan poseer una regla de producción.
    //
    // Si un símbolo no posee una regla asociada, debe conservarse sin cambios.
    //
    // Esto será especialmente importante posteriormente en los L-Systems,
    // donde símbolos como:
    //
    //      +  -  [  ]  &  ^  \  /
    //
    // pueden formar parte de la cadena sin ser necesariamente reemplazados.
    //
    //
    // -------------------------------------------------------------------------
    // DERIVACIÓN
    // -------------------------------------------------------------------------
    //
    // Además de obtener la cadena final, se almacenará el resultado de cada
    // iteración.
    //
    // Esto permite observar en la Console cómo evoluciona la gramática y
    // verificar que las reglas están siendo aplicadas correctamente.
    //

    // Metodo que es el corazon de la cadena simbolica, recibe el axioma inicial, una lista
    // de reglas, un numero de iteraciones y devuelve la cadena final
    public static string Generate(
        string axiom,
        List<LSystemRule> rules,
        int iterations,
        List<string> derivation = null)
    {
        // Comenzar desde el axioma inicial.
        string current = axiom;

        // Registrar la iteración 0 (el axioma) para el historial ( si es que es necesario)
        if (derivation != null)
        {
            derivation.Add(current);
        }


        // Crea un diccionario
        Dictionary<string, string> ruleMap =
            new Dictionary<string, string>();

        // Convierte la lista de reglas (predecesor / A -> sucesor / B) en un diccionario,
        // para poder buscar en O(1) si un simbolo tiene una regla, en vez de recorrer toda
        // la lista cada vez que se desea verificar una regla.
        if (rules != null)
        {
            foreach (LSystemRule rule in rules)
            {
                if (rule != null &&
                    !string.IsNullOrEmpty(rule.predecessor))
                {
                    ruleMap[rule.predecessor] = rule.successor;
                }
            }
        }


        // Esto es lo importante y lo que lo hace PARALELO: 
        // en cada iteración se recorre la cadena current símbolo por símbolo, y 
        // se construye una cadena nueva (next) en un StringBuilder aparte. 
        // Recién al terminar de recorrer toda la cadena, current se reemplaza por next.

        // Aplicar las reglas durante la cantidad indicada de iteraciones.
        for (int i = 0; i < iterations; i++)
        {
            // Se usa StringBuilder por el rendimiento
            // A diferencia de un string comun, un StringBuilder modifica un
            // buffer de memoria interno, reescalable, de manera directa,
            // sin crear un nuevo string de cero cada vez que se desee modificarlo
            StringBuilder next = new StringBuilder();

            // Evaluar todos los símbolos de la cadena actual de forma paralela.
            foreach (char symbol in current)
            {
                string symbolStr = symbol.ToString();

                // Si el diccionario tiene una regla para el simbolo
                if (ruleMap.ContainsKey(symbolStr)) 
                {
                    // Se agrega el simbolo correspondiente al final de "next"
                    next.Append(ruleMap[symbolStr]);
                }
                else
                {
                    // Mantener sin cambios los símbolos que no posean una regla.
                    // O sea, se agrega el simbolo, sin cambios, al final de "next"
                    // Aca se agregan los +, -, [, ], etc.
                    next.Append(symbol);
                }
            }

            // Se reemplaza la cadena anterior, por la nueva hecha en "next"
            current = next.ToString();

            // Registrar el resultado de cada iteración en derivation.
            if (derivation != null)
            {
                derivation.Add(current);
            }
        }

        // Retornar la cadena obtenida al finalizar.
        return current;
    }


    // -------------------------------------------------------------------------
    // EJECUCIÓN DESDE EL INSPECTOR
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // Su función es utilizar los parámetros configurados en el Inspector,
    // ejecutar Generate() y mostrar posteriormente la derivación.
    //

    public void GenerateExpansion()
    {
        List<string> derivation =
            new List<string>();


        string finalSequence =
            Generate(
                axiom,
                rules,
                iterations,
                derivation
            );


        if (showDerivation)
        {
            PrintDerivation(
                derivation,
                finalSequence
            );
        }
    }


    // -------------------------------------------------------------------------
    // VISUALIZACIÓN DE LA DERIVACIÓN
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //

    private void PrintDerivation(
        List<string> derivation,
        string finalSequence)
    {
        StringBuilder output =
            new StringBuilder();


        output.AppendLine(
            "===== PARALLEL GRAMMAR ====="
        );


        output.AppendLine();


        output.AppendLine(
            "AXIOM:"
        );


        output.AppendLine(
            axiom
        );


        output.AppendLine();


        output.AppendLine(
            "RULES:"
        );


        if (rules == null ||
            rules.Count == 0)
        {
            output.AppendLine(
                "(sin reglas)"
            );
        }
        else
        {
            foreach (LSystemRule rule in rules)
            {
                if (rule == null)
                {
                    continue;
                }


                output.AppendLine(
                    rule.predecessor +
                    " -> " +
                    rule.successor
                );
            }
        }


        output.AppendLine();


        output.AppendLine(
            "DERIVATION:"
        );


        for (int i = 0;
             i < derivation.Count;
             i++)
        {
            output.AppendLine(
                "Iteration " +
                i +
                ": " +
                derivation[i]
            );
        }


        output.AppendLine();


        output.AppendLine(
            "FINAL STRING:"
        );


        output.AppendLine(
            finalSequence
        );


        Debug.Log(
            output.ToString(),
            this
        );
    }
}