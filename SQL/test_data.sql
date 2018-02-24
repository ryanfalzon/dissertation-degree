CREATE TABLE test_data

SELECT t1.protein_ref_id AS protein_id, t2.sequence AS full_sequence, t1.cathfunfamilyfull_id AS functional_family, t1.region AS region
FROM protein_cathfunfamily_region t1
RIGHT JOIN

	/* Getting five records whose value of cathfunfamilyfull_id show only 20 times */
	(SELECT *, COUNT(t2.cathfunfamilyfull_id)
	FROM protein_cathfunfamily_region t2
	GROUP BY t2.cathfunfamilyfull_id
	HAVING COUNT(t2.cathfunfamilyfull_id) = 20
	LIMIT 5) AS funfamily_occurances
	
ON t1.cathfunfamilyfull_id = funfamily_occurances.cathfunfamilyfull_id

INNER JOIN protein_sequence t2
ON t1.protein_ref_id = t2.protein_ref_id
WHERE TRIM(t2.sequence) != '';