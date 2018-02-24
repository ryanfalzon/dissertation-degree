CREATE TABLE funfam_details

SELECT t1.functional_family AS cath_funfam_id, t2.cathfamily_id AS cath_family, t2.cathfunfamily_id AS cath_functional_family
FROM fyp_ryanfalzon.funfam_occurances t1
INNER JOIN fyp_dataset.gograph_cathfunfamilies t2
ON t1.functional_family = t2.cathfunfamilyfull_id;