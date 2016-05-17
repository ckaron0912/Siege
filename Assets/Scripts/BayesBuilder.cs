﻿using UnityEngine;
using System.Collections;

using Bayes;

public class BayesBuilder : MonoBehaviour
{
    public Health gate;
    public LayerMask attackerSearchLayerMask;

    public float moveSpeed = 5.0f;

    private BayesDecider bd; // THE BUILDERS WILL NEED TO SHARE THE SAME BAYESDECIDER

    private Observation currObservation;

    public bool doingIt = false;

    void Start()
    {
        gate = GameObject.Find("Gate").GetComponent<Health>();
        currObservation.outcome = -1;

        BayesDiscreteCondition enemyInWay = new BayesDiscreteCondition( "No Enemy In Way" );
        BayesDiscreteCondition enemiesAtGate = new BayesDiscreteCondition( "No Enemies At Gate" );

        BayesContinuousCondition gateHealth = new BayesContinuousCondition( "Gate Health" );
        BayesContinuousCondition distance = new BayesContinuousCondition( "Distance" );

        BayesDiscreteCondition[] discs = new BayesDiscreteCondition[ 2 ];
        discs[ 0 ] = enemyInWay;
        discs[ 1 ] = enemiesAtGate;

        BayesContinuousCondition[] conts = new BayesContinuousCondition[ 2 ];
        conts[ 0 ] = gateHealth;
        conts[ 1 ] = distance;

        bd = GameObject.Find("BayesDecider").GetComponent<BayesDeciderRef>().getBayesDecider();

        bd.SetConditions( conts, discs );
        bd.BuildStats();
    }

    void Update()
    {
    }

    void OnTriggerEnter( Collider col )
    {
        if( col.gameObject.tag == "Attacker" )
        {
            Debug.Log( "Oww!" );
            Die();
        }
        else
        {
            Debug.Log( "Repaired!" );
            doingIt = false;
            bd.AddObservation( currObservation );
        }
    }

    public void Die()
    {
        if( currObservation.outcome != -1 )
        {
            if( currObservation.outcome == 0 )
            {
                currObservation.outcome = 1;
                bd.AddObservation( currObservation ); 
            }
        }
    }

    private void DebugTestBayes()
    {
        int[] discValues = new int[ 2 ];
        discValues[ 0 ] = 0; // No enemies in way
        discValues[ 1 ] = 0; // No enemies at gate

        int[] contValues = new int[ 2 ];
        contValues[ 0 ] = 66; // gate health
        contValues[ 1 ] = 10; // dist

        if( bd.Decide( contValues, discValues ) ) // <- We pass in the arrays we built above ^
        {
            Debug.Log("Let's repair!");
        }
        else
        {
            Debug.Log( "Let's not and say we did." );
        }

        Debug.Log( "Added last case with true outcome" );
        bd.AddObservation( contValues, discValues, 0 );
        bd.Tab2Screen();
        bd.BuildStats();
    }

    public void TestBayes()
    {
        if( gate.hp < 100 )
        {
            int gateHealth = Mathf.RoundToInt( Mathf.Min( 0, gate.hp ) );

            float dist = Vector3.Distance( gate.transform.position, transform.position );

            Vector3 dir = ( gate.transform.position - transform.position ).normalized;

            RaycastHit[] hits = Physics.SphereCastAll( transform.position,
                1.0f,
                dir,
                dist - 1.0f,
                attackerSearchLayerMask );

            bool attackersInWay = false;
            for( int i = 0; i < hits.Length; i++ )
            {
                if( hits[ i ].collider.gameObject.tag == "Attacker" )
                {
                    attackersInWay = true;
                    break;
                }
            }

            float radius = 2.5f;
            Vector3 pos = gate.transform.position + dir * radius;
            Collider[] cols = Physics.OverlapSphere( pos, radius, attackerSearchLayerMask );

            bool attackersAtGate = false;
            for( int i = 0; i < cols.Length; i++ )
            {
                if( cols[ i ].gameObject.tag == "Attacker" )
                {
                    attackersAtGate = true;
                    break;
                }
            }

            int[] discValues = new int[ 2 ];
            discValues[ 0 ] = ( attackersInWay ? 1 : 0 );
            discValues[ 1 ] = ( attackersAtGate ? 1 : 0 );

            int[] contValues = new int[ 2 ];
            contValues[ 0 ] = gateHealth;
            contValues[ 1 ] = Mathf.RoundToInt( dist );

            bool doIt = bd.Decide( contValues, discValues );

            string debug = "Attackers In Path: " + attackersInWay;
            debug += "\nAttackers At Gate: " + attackersAtGate;
            debug += "\nGate Health: " + gateHealth;
            debug += "\nDistance: " + dist;
            debug += "\nRepair?: " + doIt;
            Debug.Log( debug );

            currObservation.continuousValues = contValues;
            currObservation.discreteValues = discValues;
            currObservation.outcome = ( doIt ? 0 : 1 );

            //Debug.Log( doIt );

            if( doIt )
            {
                doingIt = true;
            }
            else
            {
                StartCoroutine( MonitorDecision( dist ) );
            }
        }
    }

    private IEnumerator MonitorDecision( float dist )
    {
        bool gateAlreadyDestroyed = ( gate.hp <= 0 );
        float timeToArrive = dist / moveSpeed;
        yield return new WaitForSeconds( timeToArrive );
        //Debug.Log( "Would have arrived... wait to see..." );
        if( gate.hp <= 0 && !gateAlreadyDestroyed )
        {
            Debug.Log( "Gate would have been destroyed before arrival." );
            Debug.Log( "Good Call" );
            currObservation.outcome = 1;
        }
        else
        {
            yield return new WaitForSeconds( 5.0f );
            if( gate.hp <= 0 )
            {
                Debug.Log( "Gate could have saved!" );
                Debug.Log( "Bad Call" );
                currObservation.outcome = 0;
            }
        }

        Debug.Log( "Adding obs" );
        bd.AddObservation( currObservation );
    }
}