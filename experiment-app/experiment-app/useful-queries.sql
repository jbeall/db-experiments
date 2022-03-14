select type, COUNT(value) from random_data
GROUP BY (type)
HAVING COUNT(type) > 1
ORDER BY COUNT(type) DESC;



INSERT INTO accounts (name,debit)
SELECT value, true FROM random_data WHERE type='Account';


-- Get different types of random data
SELECT
       CompanyName.value AS CompanyName,
       Firstname.value AS Firstname,
       AnimalName.value AS AnimalName,
       PlantName.value AS PlantName,
       AppName.value AS AppName,
       Category.value AS Category,
       Equipment.value AS Equipment,
       Material.value AS Material,
       Lastname.value AS Lastname FROM
(SELECT * FROM random_data WHERE type = 'CompanyName' ORDER BY RANDOM() LIMIT 1) AS CompanyName,
(SELECT * FROM random_data WHERE type = 'Firstname' ORDER BY RANDOM() LIMIT 1) AS Firstname,
(SELECT * FROM random_data WHERE type = 'AnimalName' ORDER BY RANDOM() LIMIT 1) AS AnimalName,
(SELECT * FROM random_data WHERE type = 'PlantName' ORDER BY RANDOM() LIMIT 1) AS PlantName,
(SELECT * FROM random_data WHERE type = 'AppName' ORDER BY RANDOM() LIMIT 1) AS AppName,
(SELECT * FROM random_data WHERE type = 'Category' ORDER BY RANDOM() LIMIT 1) AS Category,
(SELECT * FROM random_data WHERE type = 'Equipment' ORDER BY RANDOM() LIMIT 1) AS Equipment,
(SELECT * FROM random_data WHERE type = 'Material' ORDER BY RANDOM() LIMIT 1) AS Material,
(SELECT * FROM random_data WHERE type = 'Lastname' ORDER BY RANDOM() LIMIT 1) AS Lastname;



