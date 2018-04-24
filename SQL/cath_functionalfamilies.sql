CREATE TABLE cath_functionalfamilies
SELECT DISTINCT(t1.cathfunfamilyfull_id) AS id, t1.cathfunfamily_id AS super_family, t1.cathfamily_id AS functional_family
FROM fyp_dataset.gograph_cathfunfamilies t1
INNER JOIN final_data t2
ON t1.cathfunfamilyfull_id = t2.functional_family;

SELECT COUNT(DISTINCT(t1.super_family)) AS super_families,
COUNT(DISTINCT(t1.id)) AS functional_families
FROM cath_functionalfamilies t1;

SELECT COUNT(DISTINCT(t1.protein_id)) AS proteins
FROM final_data t1;

SELECT t2.occurances, COUNT(t2.occurances)
FROM (
SELECT t1.functional_family, COUNT(t1.functional_family) AS occurances
FROM final_data t1
GROUP BY t1.functional_family) t2
GROUP BY occurances;