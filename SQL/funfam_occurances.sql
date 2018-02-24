CREATE TABLE funfam_occurances

SELECT functional_family AS cath_funfam_id, COUNT(*) AS occurances
FROM final_data
GROUP BY functional_family
ORDER BY occurances DESC;