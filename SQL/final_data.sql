/* CREATE the table and get the data using joins */
CREATE TABLE final_data
SELECT t1.protein_ref_id AS protein_id, SUBSTR(t2.sequence, 1, (LOCATE('\n', t2.sequence)-1)) AS sequence_header,
			TRIM(SUBSTRING(t2.sequence, LOCATE('\n', t2.sequence)+1)) AS full_sequence, t1.cathfunfamilyfull_id AS functional_family, t1.region AS region
FROM protein_cathfunfamily_region t1
INNER JOIN protein_sequence t2
ON t1.protein_ref_id = t2.protein_ref_id;

/* Remove new line characters from coloumn full_sequence */
UPDATE final_data SET full_sequence = REPLACE(REPLACE(full_sequence, '\r', ''), '\n', '');

/* Transfer table from one database to another */
CREATE TABLE final_data
SELECT * FROM fyp_dataset.final_data;

/* Remove any erroneous data from the table and put it in another table */
/* After done re-execute queries but DELETE instead of SELECT */
CREATE TABLE error_data

/* No Sequence Found For This Protein */
SELECT *, 'No Sequence Found For This Protein' AS error
FROM final_data
WHERE TRIM(full_sequence) = '';

/* Region Specified Is Beyond Sequence Length */
INSERT INTO error_data
SELECT *, 'Region Specified Is Beyond Sequence Length' AS error
FROM final_data
WHERE SUBSTRING_INDEX(region, '-', -1) > LENGTH(full_sequence);

/* Region Is Smaller Than Required */
INSERT INTO error_data
SELECT *, 'Region Is Smaller Than Required' AS error
FROM final_data
WHERE ((SUBSTRING_INDEX(region, '-', -1) - SUBSTRING_INDEX(region, '-', 1)) + 1) < 5;

/* Illegal Protein Alphabet Character */
INSERT INTO error_data
SELECT *, 'Illegal Protein Alphabet Character' AS error
FROM final_data
WHERE full_sequence LIKE '%4%' || 
full_sequence LIKE '%X%';

SELECT COUNT(*) FROM error_data GROUP BY error;