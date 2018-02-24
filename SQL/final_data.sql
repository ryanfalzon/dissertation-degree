CREATE TABLE final_data

SELECT t1.protein_ref_id AS protein_id, t2.sequence AS full_sequence, t1.cathfunfamilyfull_id AS functional_family, t1.region AS region
FROM protein_cathfunfamily_region t1
INNER JOIN protein_sequence t2
ON t1.protein_ref_id = t2.protein_ref_id
WHERE TRIM(t2.sequence) != '';